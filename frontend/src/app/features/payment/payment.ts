import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-payment',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="payment-card">
      <h3>Payment</h3>
      <p *ngIf="bookingId">Processing payment for booking #{{ bookingId }}</p>
      <button (click)="done()">Finish</button>
    </div>
  `
})
export class PaymentComponent implements OnInit {
  bookingId: string | null = null;
  constructor(private route: ActivatedRoute, private router: Router) {}

  ngOnInit() {
    this.bookingId = this.route.snapshot.paramMap.get('id');
    // auto-open: in this component we can trigger logic to show payment UI
}

  done() {
    this.router.navigate(['/']);
  }
}
