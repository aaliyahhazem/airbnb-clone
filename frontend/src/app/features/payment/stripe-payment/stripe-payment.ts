import { Component, inject, OnDestroy, OnInit, NgZone } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { PaymentService } from '../../../core/services/payment/payment-service';
import { BookingStoreService } from '../../../core/services/Booking/booking-store-service';
import { BookingService } from '../../../core/services/Booking/booking-service';
import { CreateStripePaymentVM } from '../../../core/models/payment';
import { CommonModule } from '@angular/common';
import { BehaviorSubject } from 'rxjs';

declare var Stripe: any;

@Component({
  standalone: true,
  selector: 'app-stripe-payment',
  imports: [CommonModule],
  templateUrl: './stripe-payment.html',
  styleUrl: './stripe-payment.css',
})
export class StripePayment implements OnInit, OnDestroy {
  private stripe: any;
  private elements: any;
  private card: any;
  private stripeScriptLoaded = false;
  private ngZone = inject(NgZone);

  isLoading = new BehaviorSubject<boolean>(false);
  errorMessage = '';
  successMessage = '';
  bookingId!: number;
  amount: number = 0;
  currency: string = 'egp';

  private bookingStore = inject(BookingStoreService);
  private bookingService = inject(BookingService);
  private paymentService = inject(PaymentService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  showLoginCTA = false;
  private existingClientSecret?: string | null;
  private existingPaymentIntentId?: string | null;
  intentCreationFailed = false;
  intentCreationInProgress = false;
  bookingLoaded = false;
  
  private stripePublishableKey = 'pk_test_51QcFrYAIOvv3gPwPsSer0XmyVWEEuWMzHUX6faseM6I99rQOVdqGpklBAtfUdACpZUXYBv4z1sOb1GQSgqv8Ck1200RQCnRXYc';

  ngOnInit(): void {

    this.bookingId = Number(
      this.route.snapshot.paramMap.get('bookingId') ?? 
      this.route.snapshot.paramMap.get('id')
    );

    if (!this.bookingId || isNaN(this.bookingId)) {
      this.errorMessage = 'Invalid booking ID in route.';
      console.error('Invalid bookingId:', this.bookingId);
      return;
    }

    this.loadBookingData();
    this.loadStripeScript(() => {
      // Initialize Stripe after DOM is ready
      setTimeout(() => {
        this.initializeStripe();
      }, 500);
    });
  }

  ngOnDestroy(): void {
    if (this.card) {
      try {
        this.card.destroy();
      } catch (e) {
        console.warn('Card element already destroyed');
      }
    }
  }

  loginRedirect(): void {
    this.router.navigate(['/auth/login'], {
      queryParams: { returnUrl: this.router.url }
    });
  }

  private loadStripeScript(callback: () => void): void {
    if (this.stripeScriptLoaded) {
      callback();
      return;
    }

    // Check if Stripe is already loaded
    if (typeof Stripe !== 'undefined') {
      this.stripeScriptLoaded = true;
      callback();
      return;
    }

    const script = document.createElement('script');
    script.src = 'https://js.stripe.com/v3/';
    script.async = true;
    script.onload = () => {
      this.stripeScriptLoaded = true;
      callback();
    };
    script.onerror = () => {
      console.error('Failed to load Stripe script');
      this.errorMessage = 'Failed to load payment system. Please refresh.';
    };
    document.head.appendChild(script);
  }

  private initializeStripe(): void {
    try {
      // Check if Stripe is available
      if (typeof Stripe === 'undefined') {
        console.error('Stripe not loaded');
        this.errorMessage = 'Payment system not loaded. Please refresh.';
        return;
      }

      // Check if card element exists in DOM
      const cardElement = document.getElementById('card-element');
      if (!cardElement) {
        console.error('Card element not found in DOM');
        this.errorMessage = 'Payment form not ready. Please wait...';
        // Retry after a delay
        setTimeout(() => {
          this.initializeStripe();
        }, 500);
        return;
      }

      this.stripe = Stripe(this.stripePublishableKey);
      this.elements = this.stripe.elements();
      
      // Create card element with styling
      this.card = this.elements.create('card', {
        style: {
          base: {
            fontSize: '16px',
            color: '#32325d',
            fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif',
            '::placeholder': {
              color: '#aab7c4'
            }
          },
          invalid: {
            color: '#fa755a',
            iconColor: '#fa755a'
          }
        }
      });

      // card element
      this.card.mount('#card-element');

      // clear error message on successful 
      this.ngZone.run(() => {
        this.errorMessage = '';
      });
      // Listen for card errors
      this.card.on('change', (event: any) => {
        this.ngZone.run(() => {
          if (event.error) {
            this.errorMessage = event.error.message;
          } else {
            this.errorMessage = '';
          }
        });
      });

    } catch (err) {
      this.errorMessage = 'Failed to initialize payment system. ' + (err instanceof Error ? err.message : '');
    }
  }

  private loadBookingData(): void {
    const storedBooking = this.bookingStore.currentBookingSignal?.() ?? null;

    if (storedBooking && storedBooking.id === this.bookingId) {
      this.amount = storedBooking.totalPrice ?? 0;
      this.bookingLoaded = true;

      this.existingClientSecret = this.bookingStore.getPaymentIntentClientSecret();
      this.existingPaymentIntentId = this.bookingStore.getPaymentIntentId();
    } else {
      this.isLoading.next(true);

      this.bookingService.getById(this.bookingId).subscribe({
        next: (resp) => {
          if (resp && resp.success && resp.result) {
            this.bookingStore.setCurrentBooking(resp.result);
            this.amount = resp.result.totalPrice ?? 0;
            this.bookingLoaded = true;

            if (resp.result.clientSecret && resp.result.paymentIntentId) {
              this.existingClientSecret = resp.result.clientSecret;
              this.existingPaymentIntentId = resp.result.paymentIntentId;
              this.bookingStore.setPaymentIntent(
                resp.result.clientSecret,
                resp.result.paymentIntentId
              );
            }
          } else {
            this.errorMessage = resp?.errorMessage || 'Could not load booking.';
          }

          this.isLoading.next(false);
        },
        error: (err) => {
          this.errorMessage = 'Could not load booking details. ' + (err.message ?? '');
          this.isLoading.next(false);
        }
      });
    }
  }

  pay(): void {

    if (!this.bookingLoaded) {
      this.errorMessage = 'Booking data not loaded. Please wait...';
      return;
    }

    if (!this.amount || this.amount <= 0) {
      this.errorMessage = 'Invalid booking amount. Please refresh and try again.';
      return;
    }

    //  Check if card is ready
    if (!this.card) {
      this.errorMessage = 'Payment form not ready. Please wait or refresh the page.';
      return;
    }

    this.isLoading.next(true);
    this.errorMessage = '';

    const doConfirm = async (clientSecret: string) => {
      try {
        const { paymentIntent, error } = await this.stripe.confirmCardPayment(clientSecret, {
          payment_method: { card: this.card }
        });

        if (error) {
          this.ngZone.run(() => {
            this.errorMessage = error.message || 'Payment failed.';
            this.isLoading.next(false);
          });
          return;
        }

        if (paymentIntent?.status === 'succeeded') {          
          this.ngZone.run(() => {
            this.successMessage = 'Payment successful! Redirecting...';
            this.bookingStore.updateBookingStatus(this.bookingId, 'confirmed');
            this.bookingStore.setPaymentIntent(null, null);
          });

          setTimeout(() => {
            this.ngZone.run(() => {
              this.router.navigate(['/booking/my-bookings']);
            });
          }, 1500);
        }

        this.isLoading.next(false);
      } catch (err) {
        this.ngZone.run(() => {
          this.errorMessage = 'Payment processing failed. Please try again.';
          this.isLoading.next(false);
        });
      }
    };

    if (this.existingClientSecret) {
      doConfirm(this.existingClientSecret);
      return;
    }

    const payload: CreateStripePaymentVM = {
      bookingId: this.bookingId,
      amount: this.amount,
      currency: this.currency,
      description: `Booking #${this.bookingId}`
    };

    this.paymentService.createStripeIntent(payload).subscribe({
      next: (resp) => {
        if (!resp.success || !resp.result) {
          this.errorMessage = resp.errorMessage || 'Failed to create payment intent.';
          this.isLoading.next(false);
          return;
        }

        this.bookingStore.setPaymentIntent(
          resp.result.clientSecret,
          resp.result.paymentIntentId
        );

        doConfirm(resp.result.clientSecret);
      },
      error: (err) => {
        this.errorMessage = 'Unable to process payment. Please try again.';
        this.isLoading.next(false);
      }
    });
  }

  retryCreateIntent(): void {
    this.errorMessage = '';
    this.intentCreationFailed = false;
    this.intentCreationInProgress = true;

    if (!this.amount || !this.bookingId) {
      this.errorMessage = 'Missing booking data. Please refresh the page.';
      this.intentCreationInProgress = false;
      return;
    }

    const payload: CreateStripePaymentVM = {
      bookingId: this.bookingId,
      amount: this.amount,
      currency: this.currency,
      description: `Booking #${this.bookingId}`
    };

    this.paymentService.createStripeIntent(payload).subscribe({
      next: (resp) => {
        this.intentCreationInProgress = false;

        if (!resp || !resp.success || !resp.result) {
          this.errorMessage = resp?.errorMessage || 'Failed to create payment intent.';
          this.intentCreationFailed = true;
          return;
        }

        this.bookingStore.setPaymentIntent(
          resp.result.clientSecret,
          resp.result.paymentIntentId
        );

        this.existingClientSecret = resp.result.clientSecret;
        this.existingPaymentIntentId = resp.result.paymentIntentId;
        this.intentCreationFailed = false;

      },
      error: (err) => {
        this.errorMessage = 'Failed to create payment intent. Try again.';
        this.intentCreationFailed = true;
        this.intentCreationInProgress = false;
      }
    });
  }
}