import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ListingService } from '../../../core/services/listings/listing.service';
import { ListingOverviewVM } from '../../../core/models/listing.model';

@Component({
  selector: 'app-listings-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, TranslateModule],
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
  pageSize = 14;
  totalCount = signal<number>(0);

  // search + filters
  search = signal<string>('');
  location = signal<string>('');
  maxPrice = signal<number | null>(null);
  minRating = signal<number | null>(null);
  isApproved = signal<string>(''); // '', approved, not-approved

  // computed
  totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize)));

  paginationPages = computed<(number | string)[]>(() => {
    const total = this.totalPages();
    const current = this.currentPage();
    const pages: (number | string)[] = [];

    if (total <= 7) {
      for (let i = 1; i <= total; i++) {
        pages.push(i);
      }
    } else {
      pages.push(1);
      if (current > 3) pages.push('...');
      for (let i = Math.max(2, current - 1); i <= Math.min(total - 1, current + 1); i++) {
        pages.push(i);
      }
      if (current < total - 2) pages.push('...');
      pages.push(total);
    }
    return pages;
  });

  locations = computed<string[]>(() => {
    return [
      'Cairo',
      'Giza',
      'Alexandria',
      'Luxor',
      'Aswan',
      'Sharm El Sheikh',
      'Hurghada',
      'Mansoura',
      'Tanta',
      'Fayoum',
      'Ismailia',
      'Port Said',
      'Suez',
      'Zagazig',
      'Qena',
      'Sohag',
      'Assiut',
      'Bani Suef',
      'Minya',
      'Damanhour',
      'Kafr El Sheikh',
      'Damietta',
      'Marsa Matruh',
      'North Sinai',
      'South Sinai',
      'Red Sea',
      'New Cairo',
      'Obour',
      'Sheikh Zayed',
      '6th of October City'
    ].sort((a, b) => a.localeCompare(b));
  });

  private normalize(input?: string): string {
    if (!input) return '';
    let s = String(input).trim().toLowerCase();

    s = s.replace(/[\u0610-\u061A\u064B-\u065F\u06D6-\u06ED]/g, '');
    s = s.replace(/أ|إ|آ/g, 'ا');
    s = s.replace(/ة/g, 'ه');
    s = s.replace(/ى/g, 'ي');
    s = s.replace(/ؤ/g, 'و');
    s = s.replace(/ئ/g, 'ي');
    s = s.replace(/ک/g, 'ك').replace(/ی/g, 'ي');
    s = s.replace(/[^0-9a-z\u0600-\u06FF\s]/g, '');
    s = s.replace(/\s+/g, ' ').trim();

    return s;
  }

  filtered = computed<ListingOverviewVM[]>(() => {
    const data = this.listings();
    if (!data || !Array.isArray(data) || data.length === 0) return [];

    const rawQuery = this.search().trim();
    const rawLoc = this.location().trim();
    const approvedFilter = this.isApproved();
    const maxP = this.maxPrice();
    const minR = this.minRating();

    const q = this.normalize(rawQuery);
    const locNormalized = this.normalize(rawLoc);

    return data.filter(l => {
      const title = this.normalize(l.title);
      const locationVal = this.normalize(l.location);
      const description = this.normalize(l.description ?? '');

      const matchesSearch =
        !q ||
        title.includes(q) ||
        locationVal.includes(q) ||
        description.includes(q);

      const matchesLocation =
        !locNormalized || locationVal.includes(locNormalized);

      const matchesApproval =
        approvedFilter === '' ||
        (approvedFilter === 'approved' && l.isApproved === true) ||
        (approvedFilter === 'not-approved' && l.isApproved === false);

      const priceOk =
        (maxP === null || l.pricePerNight <= maxP);

      const ratingOk =
        (minR === null || (l.averageRating ?? 0) >= minR);

      return matchesSearch && matchesLocation && matchesApproval && priceOk && ratingOk;
    });
  });

  ngOnInit() {
    this.loadListings();
  }

  loadListings() {
    this.loading.set(true);
    this.error.set('');

    this.listingService.getHostListings(this.currentPage(), this.pageSize).subscribe({
      next: (response) => {
        if (!response.isError) {
          this.listings.set(response.data || []);
          this.totalCount.set(response.totalCount || 0);
        } else {
          this.error.set(response.message || 'Failed to load your listings');
        }
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Error loading your listings: ' + (err.message || 'Unknown error'));
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
    this.maxPrice.set(null);
    this.minRating.set(null);
    this.isApproved.set('');
    this.currentPage.set(1);
  }

  goToPage(page: number) {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
      this.loadListings();
    }
  }

  nextPage() {
    if (this.currentPage() < this.totalPages()) {
      this.currentPage.update(p => p + 1);
      this.loadListings();
    }
  }

  prevPage() {
    if (this.currentPage() > 1) {
      this.currentPage.update(p => p - 1);
      this.loadListings();
    }
  }

  trackById(index: number, item: ListingOverviewVM): number {
    return item.id ?? index;
  }
}