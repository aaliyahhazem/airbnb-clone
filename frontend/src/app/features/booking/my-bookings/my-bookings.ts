import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { BookingService } from '../../../core/services/Booking/booking-service';
import { BookingStoreService } from '../../../core/services/Booking/booking-store-service';
import { GetBookingVM } from '../../../core/models/booking';
import { CommonModule } from '@angular/common';
import Swal from 'sweetalert2';

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
  filter = signal<'all' | 'upcoming' | 'past' | 'cancelled'>('all');

  ilteredBookings = computed(() => {
    const f = this.filter();
    const all = this.bookings();

    const now = new Date();

    if (f === 'all') return all;
    if (f === 'cancelled') return all.filter(b => b.bookingStatus === 'cancelled');
    if (f === 'upcoming') return all.filter(b => new Date(b.checkInDate) >= now);
    if (f === 'past') return all.filter(b => new Date(b.checkOutDate) < now);

    return all;
  });
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
            Swal.fire({
            icon: 'error',
            title: 'Loading Failed',
            text: response.errorMessage || 'Failed to load bookings',
            confirmButtonColor: '#d00'
          });
        }
      },
      error: (error) => {
        this.isLoading.set(false);
        this.errorMessage.set('An error occurred while loading bookings');
        console.error('Bookings loading error:', error);
         Swal.fire({
          icon: 'error',
          title: 'Error',
          text: 'An error occurred while loading bookings',
          confirmButtonColor: '#d00'
        });
      }
    });
  }
  cancelBooking(bookingId: number): void { Swal.fire({
      title: 'Cancel Booking?',
      text: 'Are you sure you want to cancel this booking?',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d00',
      cancelButtonColor: '#6c757d',
      confirmButtonText: 'Yes, cancel it',
      cancelButtonText: 'No, keep it'
    }).then((result) => {
      if (result.isConfirmed) {
        // Show loading
        Swal.fire({
          title: 'Cancelling...',
          text: 'Please wait',
          allowOutsideClick: false,
          didOpen: () => {
            Swal.showLoading();
          }
        });

        this.bookingService.cancelBooking(bookingId).subscribe({
          next: (response) => {
            if (response.success) {
              Swal.fire({
                icon: 'success',
                title: 'Cancelled!',
                text: 'Your booking has been cancelled successfully.',
                confirmButtonColor: '#28a745'
              });
              this.loadMyBookings(); // Reload bookings
            } else {
              Swal.fire({
                icon: 'error',
                title: 'Cancellation Failed',
                text: response.errorMessage || 'Failed to cancel booking',
                confirmButtonColor: '#d00'
              });
            }
          },
          error: (error) => {
          alert('An error occurred while cancelling booking');
            console.error('Booking cancellation error:', error);
            Swal.fire({
              icon: 'error',
              title: 'Error',
              text: 'An error occurred while cancelling booking',
              confirmButtonColor: '#d00'
            });
          }
        });
      }
    });
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