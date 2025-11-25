import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ListingService } from '../../../core/services/listings/listing.service';
import { ListingOverviewVM } from '../../../core/models/listing.model';

@Component({
  selector: 'app-listings-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './listing-list.html',
  styleUrls: ['./listing-list.css'],
})
export class ListingsList implements OnInit {
  private listingService = inject(ListingService);

  // state
  listings = signal<ListingOverviewVM[]>([]);
  loading = signal<boolean>(false);
  error = signal<string>('');
  currentPage = signal<number>(1);
  pageSize = 12;
  totalCount = signal<number>(0);

  // search + filters
  search = signal<string>('');
  location = signal<string>('');
  minPrice = signal<number | null>(null);
  maxPrice = signal<number | null>(null);
  minRating = signal<number | null>(null);

  // computed
  totalPages = computed(() => Math.ceil(this.totalCount() / this.pageSize));

  locations = computed<string[]>(() => {
    const data = this.listings();
    if (!data || !Array.isArray(data)) return [];
    const set = new Set(data.map(l => (l.location || '').trim()).filter(Boolean));
    return Array.from(set).sort((a, b) => a.localeCompare(b));
  });

  filtered = computed<ListingOverviewVM[]>(() => {
    const data = this.listings();
    if (!data || !Array.isArray(data) || data.length === 0) return [];
    
    const term = this.search().trim().toLowerCase();
    const loc = this.location().trim().toLowerCase();
    const minP = this.minPrice();
    const maxP = this.maxPrice();
    const minR = this.minRating();

    return data.filter(l => {
      const matchesSearch =
        !term ||
        (l.title ?? '').toLowerCase().includes(term) ||
        (l.location ?? '').toLowerCase().includes(term);

      const matchesLocation = !loc || (l.location ?? '').toLowerCase() === loc;

      const priceOk =
        (minP === null || l.pricePerNight >= minP) &&
        (maxP === null || l.pricePerNight <= maxP);

      const ratingOk = (minR === null || (l.averageRating ?? 0) >= minR);

      return matchesSearch && matchesLocation && priceOk && ratingOk;
    });
  });

  ngOnInit() {
    this.loadListings();
  }

  loadListings() {
    this.loading.set(true);
    this.error.set('');
    console.log('📍 Loading listings from API...');

    this.listingService.getPaged(this.currentPage(), this.pageSize).subscribe({
      next: (response) => {
        console.log('✅ API Response:', response);
        if (!response.isError) {
          console.log('📋 Listings count:', response.data?.length || 0);
          this.listings.set(response.data || []);
          this.totalCount.set(response.totalCount || 0);
        } else {
          this.error.set(response.message || 'Failed to load listings');
        }
        this.loading.set(false);
      },
      error: (err) => {
        console.error('❌ API Error:', err);
        this.error.set('Error loading listings: ' + (err.message || 'Unknown error'));
        this.loading.set(false);
      }
    });
  }

  onDelete(id: number) {
    if (!confirm('Delete this listing?')) return;
    
    this.listingService.delete(id).subscribe({
      next: (response) => {
        if (!response.isError) {
          this.listings.update(list => list.filter(l => l.id !== id));
        } else {
          this.error.set(response.message || 'Failed to delete listing');
        }
      },
      error: (err) => {
        this.error.set('Error deleting listing: ' + (err.message || 'Unknown error'));
      }
    });
  }

  resetFilters() {
    this.search.set('');
    this.location.set('');
    this.minPrice.set(null);
    this.maxPrice.set(null);
    this.minRating.set(null);
    this.currentPage.set(1);
  }

  goToPage(page: number) {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
      this.loadListings();
    }
  }

  trackById(index: number, item: ListingOverviewVM): number {
    return item.id ?? index;
  }
}
