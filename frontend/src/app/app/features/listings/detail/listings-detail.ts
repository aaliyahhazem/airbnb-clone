import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ListingsService } from '../services/listings';
import { Listing } from '../models/listing.model';

@Component({
  selector: 'app-listings-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './listings-detail.html',
  styleUrls: ['./listings-detail.css'],
})
export class ListingsDetail {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private service = inject(ListingsService);

  listing?: Listing;

  constructor() {
    const idParam = this.route.snapshot.paramMap.get('id');
    const id = idParam ? +idParam : NaN;
    this.listing = Number.isFinite(id) ? this.service.getById(id) : undefined;
    // If you also add the guard below, this fallback will rarely trigger.
  }

  goBack() {
    this.router.navigate(['/listings']);
  }
}
