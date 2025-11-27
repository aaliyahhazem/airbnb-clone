import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="admin-dashboard">
      <h2>Admin Dashboard</h2>
      <p>Manage users, listings, bookings, notifications, reviews and payments from here.</p>
      <!-- In future: list of admin endpoints/actions -->
    </div>
  `
})
export class Dashboard {}
