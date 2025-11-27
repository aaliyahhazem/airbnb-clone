import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

@Component({
  selector: 'app-booking',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="booking-card">
      <h3>Create Booking</h3>
      <form (ngSubmit)="submit()">
        <input name="listingId" [(ngModel)]="listingId" placeholder="Listing ID" />
        <input name="from" [(ngModel)]="from" placeholder="From (YYYY-MM-DD)" />
        <input name="to" [(ngModel)]="to" placeholder="To (YYYY-MM-DD)" />
        <button type="submit">Book</button>
      </form>
    </div>
  `
})
export class BookingComponent {
  listingId = '';
  from = '';
  to = '';

  constructor(private router: Router) {}

  submit() {
    // In real app call booking API; here navigate to payment with a fake booking id
    const bookingId = Math.floor(Math.random() * 100000);
    this.router.navigate(['/payment', bookingId]);
  }
}
