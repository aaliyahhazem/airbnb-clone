import { Injectable, signal } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { GetBookingVM } from '../../models/booking';

@Injectable({
  providedIn: 'root',
})
export class BookingStoreService {
  private myBookings = signal<GetBookingVM[]>([]);
  private hostBookings = signal<GetBookingVM[]>([]);
  private currentBooking = signal<GetBookingVM | null>(null);
  // Temporary holder for a clientSecret and payment intent id for the currently created booking
  private currentPaymentIntentClientSecret = signal<string | null>(null);
  private currentPaymentIntentId = signal<string | null>(null);

  // Signals for reactive state management
  readonly myBookingsSignal = this.myBookings.asReadonly();
  readonly hostBookingsSignal = this.hostBookings.asReadonly();
  readonly currentBookingSignal = this.currentBooking.asReadonly();

  setMyBookings(bookings: GetBookingVM[]): void {
    this.myBookings.set(bookings);
  }

  setHostBookings(bookings: GetBookingVM[]): void {
    this.hostBookings.set(bookings);
  }

  setCurrentBooking(booking: GetBookingVM): void {
    this.currentBooking.set(booking);
  }

  setPaymentIntent(clientSecret: string | null, intentId?: string | null): void {
    this.currentPaymentIntentClientSecret.set(clientSecret ?? null);
    this.currentPaymentIntentId.set(intentId ?? null);
  }

  getPaymentIntentClientSecret(): string | null {
    return this.currentPaymentIntentClientSecret();
  }

  getPaymentIntentId(): string | null {
    return this.currentPaymentIntentId();
  }

  addBooking(booking: GetBookingVM): void {
    this.myBookings.update(bookings => [...bookings, booking]);
  }

  updateBookingStatus(bookingId: number, status: string): void {
    this.myBookings.update(bookings =>
      bookings.map(booking =>
        booking.id === bookingId ? { ...booking, bookingStatus: status } : booking
      )
    );

    this.hostBookings.update(bookings =>
      bookings.map(booking =>
        booking.id === bookingId ? { ...booking, bookingStatus: status } : booking
      )
    );

    const current = this.currentBooking();
    if (current && current.id === bookingId) {
      this.currentBooking.set({ ...current, bookingStatus: status });
    }
  }

  clearCurrentBooking(): void {
    this.currentBooking.set(null);
  }
}
