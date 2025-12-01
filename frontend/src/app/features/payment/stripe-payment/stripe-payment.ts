import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { PaymentService } from '../../../core/services/payment/payment-service';
import { BookingStoreService } from '../../../core/services/Booking/booking-store-service';
import { BookingService } from '../../../core/services/Booking/booking-service';
import { CreatePaymentIntentVm, CreateStripePaymentVM } from '../../../core/models/payment';
import { AuthService } from '../../../core/services/auth.service';
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
  isLoading = new BehaviorSubject<boolean>(false);
  errorMessage = '';
  successMessage = '';
  bookingId!: number;
  amount!: number;
  private bookingStore = inject(BookingStoreService);
  private bookingService = inject(BookingService);
  private authService = inject(AuthService);
    showLoginCTA = false;
  private existingClientSecret?: string | null;
  private existingPaymentIntentId?: string | null;
  // keep lifecycle state of intent creation
  intentCreationFailed = false;
  intentCreationInProgress = false;

  private stripePublishableKey = 'pk_test_51QcFrYAIOvv3gPwPsSer0XmyVWEEuWMzHUX6faseM6I99rQOVdqGpklBAtfUdACpZUXYBv4z1sOb1GQSgqv8Ck1200RQCnRXYc';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private paymentService: PaymentService
  ) { }
  ngOnInit(): void {
    this.bookingId = Number(this.route.snapshot.paramMap.get('bookingId'));
    // Try to use clientSecret stored by the booking flow first
    this.existingClientSecret = this.bookingStore.getPaymentIntentClientSecret();
    this.existingPaymentIntentId = this.bookingStore.getPaymentIntentId();

    const storedBooking = this.bookingStore.currentBookingSignal?.() ?? null;

    // Read amount robustly: prefer explicit query param when present (non-empty),
    // otherwise use stored booking, and support multiple casing shapes returned by backend.
    const rawAmount = this.route.snapshot.queryParamMap.get('amount');
    const parsed = rawAmount !== null && rawAmount !== '' ? Number(rawAmount) : NaN;

    if (!isNaN(parsed)) {
      this.amount = parsed;
    } else if (storedBooking) {
      // backend might return camelCase or PascalCase - accept both shapes
      this.amount = (storedBooking as any).totalPrice ?? (storedBooking as any).TotalPrice ?? 0;
    } else {
      this.amount = 0;
    }

    if (!this.bookingId) {
      this.errorMessage = 'Missing bookingId in route.';
      console.warn('[StripePayment] missing bookingId');
      return;
    }

    if (!this.amount && !this.existingClientSecret) {
      // attempt to load booking details from API when navigated directly (deep-link)
      this.bookingService.getById(this.bookingId).subscribe({
        next: (resp) => {
          if (resp && resp.success && resp.result) {
            // store the booking so other parts of the app can pick it up
            this.bookingStore.setCurrentBooking(resp.result);
            // accept both casing shapes just in case
            this.amount = (resp.result as any).totalPrice ?? (resp.result as any).TotalPrice ?? this.amount;
            // After we loaded booking data try to create intent when user clicks Pay or auto when they retry
          } else {
            // Show friendly error
            this.errorMessage = resp?.errorMessage || 'Missing booking amount and no clientSecret.';
          }
        },
        error: (err) => {
          console.error('[StripePayment] failed to load booking', err);
          // Try to surface useful information returned by server (authorization, not found, etc.)
          let userFriendly = 'Could not load booking details.';
          try {
            if (err && err.status === 401) {
              userFriendly = 'You must be logged in to view this booking.';
              this.showLoginCTA = true;
            }
            else if (err && err.error) userFriendly = (err.error?.errorMessage ?? err.error?.message ?? JSON.stringify(err.error));
            else if (err && err.message) userFriendly = err.message;
          } catch { }
          this.errorMessage = `Missing booking amount (and no stored clientSecret). ${userFriendly}`;
        }
      });
    }

    this.loadStripeScript(() => this.initializeStripe());
  }

  loginRedirect() {
    try { this.router.navigate(['/auth/login'], { queryParams: { returnUrl: this.router.url } }); } catch { this.router.navigate(['/auth/login']); }
  }
  ngOnDestroy(): void {
    if (this.card) this.card.destroy();
  }
  private loadStripeScript(callback: () => void) {
    if (this.stripeScriptLoaded) {
      callback();
      return;
    }

    const script = document.createElement('script');
    script.src = 'https://js.stripe.com/v3/';
    script.onload = () => {
      this.stripeScriptLoaded = true;
      callback();
    };
    document.body.appendChild(script);
  }
  private initializeStripe() {
    this.stripe = Stripe(this.stripePublishableKey);
    this.elements = this.stripe.elements();

    this.card = this.elements.create('card');
    this.card.mount('#card-element');
  }
  pay() {
    this.isLoading.next(true);
    this.errorMessage = '';

    const doConfirm = async (clientSecret: string) => {
      const { paymentIntent, error } = await this.stripe.confirmCardPayment(clientSecret, {
        payment_method: {
          card: this.card
        }
      });

        if (error) {
          this.errorMessage = error.message || 'Payment failed.';
          this.isLoading.next(false);
          return;
        }

        if (paymentIntent && paymentIntent.status === 'succeeded') {
          this.successMessage = 'Payment successful! Redirecting...';

          setTimeout(() => {
            this.router.navigate(['/booking/my-bookings']);
          }, 1500);
        }
        // Call backend to confirm payment immediately (redundant with webhook but gives immediate DB update)
          try {
          const txId = paymentIntent?.id;
          if (txId) {
            this.paymentService.confirmPayment({ bookingId: this.bookingId, transactionId: txId }).subscribe({
              next: (resp) => {
                  // backend confirmPayment returns a payload; check result presence rather than relying on 'success' property
                  if (resp && (resp as any).result) {
                    this.bookingStore.updateBookingStatus(this.bookingId, 'confirmed');
                    // clear the stored client secret after successful payment
                    try { this.bookingStore.setPaymentIntent(null, null); } catch {}
                  }
              },
              error: (err) => console.warn('[StripePayment] confirmPayment failed', err)
            });
          }
        } catch (e) {
          console.warn('[StripePayment] error calling confirmPayment', e);
        }
        this.isLoading.next(false);
    };

    // If we already have a prepared clientSecret from the booking flow, use it
    const storedSecret = this.bookingStore.getPaymentIntentClientSecret();
    if (storedSecret) {
      doConfirm(storedSecret);
      return;
    }

    // Otherwise create a new intent now
    const payload: CreateStripePaymentVM = { bookingId: this.bookingId, amount: this.amount, currency: 'usd', description: `Booking #${this.bookingId}` };
    this.paymentService.createStripeIntent(payload).subscribe({
      next: (resp) => {
        if (!resp.success || !resp.result) {
          this.errorMessage = resp.errorMessage || 'Failed to create payment intent.';
          this.isLoading.next(false);
          return;
        }
        doConfirm(resp.result.clientSecret);
      },
      error: (err) => {
        console.error('[StripePayment] createStripeIntent failed', err);
        this.errorMessage = 'Unable to process payment. Tap Retry to create the PaymentIntent again.';
        this.intentCreationFailed = true;
        this.intentCreationInProgress = false;
        this.isLoading.next(false);
      }
    });
  }

  // Exposed method: explicitly create the PaymentIntent and store the clientSecret
  retryCreateIntent() {
    // reset failure state
    this.errorMessage = '';
    this.intentCreationFailed = false;
    this.intentCreationInProgress = true;

    const amountToUse = this.amount;
    if (!amountToUse || !this.bookingId) {
      this.errorMessage = 'Missing bookingId or amount to create PaymentIntent.';
      this.intentCreationInProgress = false;
      return;
    }

    const payload: CreateStripePaymentVM = { bookingId: this.bookingId, amount: amountToUse, currency: 'usd', description: `Booking #${this.bookingId}` };
    this.paymentService.createStripeIntent(payload).subscribe({
      next: (resp) => {
        this.intentCreationInProgress = false;
        if (!resp || !resp.success || !resp.result) {
          this.errorMessage = resp?.errorMessage || 'Failed to create payment intent.';
          this.intentCreationFailed = true;
          return;
        }
        const clientSecret = resp.result.clientSecret;
        const paymentIntentId = resp.result.paymentIntentId;
        // store in booking store (so pay() will use it)
        try { this.bookingStore.setPaymentIntent(clientSecret, paymentIntentId); } catch {}
        this.intentCreationFailed = false;
        // optionally auto-confirm payment right away if user already clicked Pay?
      },
      error: (err) => {
        console.error('[StripePayment] retry createIntent failed', err);
        this.errorMessage = 'Failed to create payment intent. Try again.';
        this.intentCreationFailed = true;
        this.intentCreationInProgress = false;
      }
    });
  }
}