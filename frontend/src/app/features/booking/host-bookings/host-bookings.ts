import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { BookingService } from '../../../core/services/Booking/booking-service';
import { BookingStoreService } from '../../../core/services/Booking/booking-store-service';
import { GetBookingVM } from '../../../core/models/booking';

@Component({
  selector: 'app-host-bookings',
  imports: [CommonModule],
  templateUrl: './host-bookings.html',
  styleUrl: './host-bookings.css',
})
export class HostBookings implements OnInit {
 private bookingService = inject(BookingService);
  private bookingStore = inject(BookingStoreService);

  bookings = signal<GetBookingVM[]>([]);
  isLoading = signal(false);
  errorMessage = signal('');

  ngOnInit(): void {
    this.loadHostBookings();
  }
 loadHostBookings(): void {
    this.isLoading.set(true);
    this.errorMessage.set('');

    this.bookingService.getHostBookings().subscribe({
      next: (response) => {
        this.isLoading.set(false);
        if (response.success && response.result) {
          this.bookings.set(response.result);
          this.bookingStore.setHostBookings(response.result);
        } else {
          this.errorMessage.set(response.errorMessage || 'Failed to load host bookings');
        }
      },
      error: (error) => {
        this.isLoading.set(false);
        this.errorMessage.set('An error occurred while loading host bookings');
        console.error('Host bookings loading error:', error);
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
    getTotalRevenue(): number {
    return this.bookings()
      .filter(booking => booking.bookingStatus === 'confirmed')
      .reduce((sum, booking) => sum + booking.totalPrice, 0);
  }

  getConfirmedBookingsCount(): number {
    return this.bookings().filter(booking => booking.bookingStatus === 'confirmed').length;
  }
}
