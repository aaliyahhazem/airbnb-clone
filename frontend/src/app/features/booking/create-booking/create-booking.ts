import { Component, EventEmitter, inject, Input, OnInit, Output } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BookingService } from '../../../core/services/Booking/booking-service';
import { ListingService } from '../../../core/services/listings/listing.service';
import { PaymentService } from '../../../core/services/payment/payment-service';
import { CreateBookingVM } from '../../../core/models/booking';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { BookingStoreService } from '../../../core/services/Booking/booking-store-service';
import { AuthService } from '../../../core/services/auth.service';
import { catchError, map, of, switchMap, throwError } from 'rxjs';

@Component({
  selector: 'app-create-booking',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './create-booking.html',
  styleUrl: './create-booking.css',
})
export class CreateBooking implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private bookingService = inject(BookingService);
  private paymentService = inject(PaymentService);
  private listingService = inject(ListingService);
  private authService = inject(AuthService);
  private bookingStore = inject(BookingStoreService);
  private fb = inject(FormBuilder);
  bookingForm!: FormGroup;
  // listingId!: number;
  isLoading = false;
  errorMessage = '';
  today!: string;
  @Input() listingId!: number;
  @Input() listingPrice: number = 100;
  @Input() listingMaxGuests?: number;
  @Output() bookingCreated = new EventEmitter<any>();
  @Output() bookingCancelled = new EventEmitter<void>();
  ngOnInit(): void {
    if (!this.listingId) {
      this.listingId = Number(this.route.snapshot.paramMap.get('id'));
    }
    this.today = new Date().toISOString().split('T')[0];
    // (removed dev debugging of token payload) 

    // If the parent did not pass prices / maxGuests (route-based), fetch listing details first
    if (!this.listingPrice || !this.listingMaxGuests) {
      if (this.listingId) {
        this.listingService.getById(this.listingId).subscribe((res) => {
          if (!res.isError && res.data) {
            this.listingPrice = res.data.pricePerNight ?? this.listingPrice;
            this.listingMaxGuests = res.data.maxGuests ?? this.listingMaxGuests;
          }
          this.initForm();
        }, (err) => {
          // still init form with defaults
          console.warn('Failed to load listing details for booking', err);
          this.initForm();
        });
      } else {
        this.initForm();
      }
    } else {
      this.initForm();
    }
  }

  private initForm(): void {
    const today = new Date().toISOString().split('T')[0];
    const tomorrow = new Date(Date.now() + 86400000).toISOString().split('T')[0];

    this.bookingForm = this.fb.group({
      checkInDate: [today, [Validators.required]],
      checkOutDate: [tomorrow, [Validators.required]],
      guests: [1, [Validators.required, Validators.min(1), Validators.max(this.listingMaxGuests ?? 10)]],
      paymentMethod: ['stripe', [Validators.required]]
    });

    // cross-field validation: checkOut must be after checkIn
    this.bookingForm.setValidators(() => {
      const checkIn = new Date(this.bookingForm.get('checkInDate')?.value);
      const checkOut = new Date(this.bookingForm.get('checkOutDate')?.value);
      if (checkOut <= checkIn) return { invalidDates: true };
      return null;
    });
  }
  calculateTotalPrice(): number {
    const checkIn = new Date(this.bookingForm.get('checkInDate')?.value);
    const checkOut = new Date(this.bookingForm.get('checkOutDate')?.value);
    const nights = Math.ceil((checkOut.getTime() - checkIn.getTime()) / (1000 * 60 * 60 * 24));
    return nights * (this.listingPrice || 0);
  }

onSubmit(): void {
  if (this.bookingForm.valid) {
    this.isLoading = true;
    this.errorMessage = '';

    const bookingData: CreateBookingVM = {
      listingId: this.listingId,
      ...this.bookingForm.value
    };

    // ✅ BACKEND NOW RETURNS EVERYTHING IN ONE CALL
    this.bookingService.createBooking(bookingData).subscribe({
      next: (res) => {
        if (!res.success || !res.result) {
          this.errorMessage = res.errorMessage || 'Failed to create booking';
          this.isLoading = false;
          return;
        }

        const booking = res.result;
        this.bookingStore.setCurrentBooking(booking);
        this.bookingCreated.emit(booking);

        // ✅ USE CLIENT SECRET FROM BOOKING RESPONSE
        if (booking.clientSecret) {
          this.bookingStore.setPaymentIntent(booking.clientSecret, booking.paymentIntentId);
          this.router.navigate(['/booking/payment', booking.id]);
        } else {
          // Fallback for non-Stripe payments
          this.router.navigate(['/booking/payment', booking.id], { 
            queryParams: { amount: booking.totalPrice } 
          });
        }
        
        this.isLoading = false;
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = this.errorMessage || 'An error occurred while creating the booking.';
      }
    });
  }
}
  onCancel(): void {
    this.bookingCancelled.emit();
  }
  getNumberOfNights(): number {
    const checkIn = new Date(this.bookingForm.get('checkInDate')?.value);
    const checkOut = new Date(this.bookingForm.get('checkOutDate')?.value);
    return Math.ceil((checkOut.getTime() - checkIn.getTime()) / (1000 * 60 * 60 * 24));
  }
  get minCheckOutDate(): string {
    const checkInDate = this.bookingForm.get('checkInDate')?.value;
    if (checkInDate) {
      const nextDay = new Date(checkInDate);
      nextDay.setDate(nextDay.getDate() + 1);
      return nextDay.toISOString().split('T')[0];
    }
    return '';
  }
}