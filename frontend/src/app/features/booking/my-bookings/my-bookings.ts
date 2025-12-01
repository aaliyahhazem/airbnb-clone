import { Component, inject, OnInit, signal } from '@angular/core';
import { BookingService } from '../../../core/services/Booking/booking-service';
import { BookingStoreService } from '../../../core/services/Booking/booking-store-service';
import { GetBookingVM } from '../../../core/models/booking';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-my-bookings',
  imports: [CommonModule],
  templateUrl: './my-bookings.html',
  styleUrl: './my-bookings.css',
})
export class MyBookings implements OnInit {
  private bookingService = inject(BookingService);
  private bookingStore = inject(BookingStoreService);

  bookings = signal<GetBookingVM[]>([]);
  isLoading = signal(false);
  errorMessage = signal('');

  ngOnInit(): void {
    this.loadMyBookings();
  }
  loadMyBookings(): void {
    this.isLoading.set(true);
    this.errorMessage.set('');

    this.bookingService.getMyBookings().subscribe({
      next: (response) => {
        this.isLoading.set(false);
        if (response.success && response.result) {
          this.bookings.set(response.result);
          this.bookingStore.setMyBookings(response.result);
        } else {
          this.errorMessage.set(response.errorMessage || 'Failed to load bookings');
        }
      },
      error: (error) => {
        this.isLoading.set(false);
        this.errorMessage.set('An error occurred while loading bookings');
        console.error('Bookings loading error:', error);
      }
    });
  }
  cancelBooking(bookingId: number): void {
    if (confirm('Are you sure you want to cancel this booking?')) {
      this.bookingService.cancelBooking(bookingId).subscribe({
        next: (response) => {
          if (response.success) {
            this.loadMyBookings(); // Reload bookings
          } else {
            alert(response.errorMessage || 'Failed to cancel booking');
          }
        },
        error: (error) => {
          alert('An error occurred while cancelling booking');
          console.error('Booking cancellation error:', error);
        }
      });
    }
  }
  getStatusBadgeClass(status: string): string {
    switch (status.toLowerCase()) {
      case 'confirmed':
        return 'bg-success';
      case 'pending':
        return 'bg-warning';
      case 'cancelled':
        return 'bg-danger';
      default:
        return 'bg-secondary';
    }
  }
}