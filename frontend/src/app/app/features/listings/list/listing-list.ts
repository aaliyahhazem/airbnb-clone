import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ListingsService } from '../services/listings';
import { Listing } from '../models/listing.model';

@Component({
  selector: 'app-listings-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './listing-list.html',
  styleUrls: ['./listing-list.css'],
})
export class ListingsList {
  private service = inject(ListingsService);

  // source data
  listings = this.service.listings;

  // search + filters
  search = signal<string>('');
  location = signal<string>('');      // '' = all
  minPrice = signal<number | null>(null);
  maxPrice = signal<number | null>(null);
  minRating = signal<number | null>(null);

  // locations for dropdown (unique, sorted)
  locations = computed<string[]>(() => {
    const set = new Set(this.listings().map(l => (l.location || '').trim()).filter(Boolean));
    return Array.from(set).sort((a, b) => a.localeCompare(b));
  });

  // filtered result
  filtered = computed<Listing[]>(() => {
    const term = this.search().trim().toLowerCase();
    const loc = this.location().trim().toLowerCase();
    const minP = this.minPrice();
    const maxP = this.maxPrice();
    const minR = this.minRating();

    return this.listings().filter(l => {
      // search text
      const matchesSearch =
        !term ||
        (l.title ?? '').toLowerCase().includes(term) ||
        (l.location ?? '').toLowerCase().includes(term) ||
        (l.description ?? '').toLowerCase().includes(term);

      // location
      const matchesLocation = !loc || (l.location ?? '').toLowerCase() === loc;

      // price
      const priceOk =
        (minP === null || l.price >= minP) &&
        (maxP === null || l.price <= maxP);

      // rating
      const ratingOk = (minR === null || (l.rating ?? 0) >= minR);

      return matchesSearch && matchesLocation && priceOk && ratingOk;
    });
  });

  onDelete(id: number) {
    if (!confirm('Delete this listing?')) return;
    this.service.remove(id);
  }

  resetFilters() {
    this.search.set('');
    this.location.set('');
    this.minPrice.set(null);
    this.maxPrice.set(null);
    this.minRating.set(null);
  }
  trackById(index: number, item: Listing): number {
  return item.id ?? index;
}
}
