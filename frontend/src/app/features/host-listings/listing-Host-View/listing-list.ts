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
  pageSize = 6;
  totalCount = signal<number>(0);

  // search + filters
  search = signal<string>('');
  destination = signal<string>('');
  type = signal<string>('');
  maxPrice = signal<number | null>(null);
  minRating = signal<number | null>(null);
  isApproved = signal<string>(''); // '', approved, not-approved

  // delete confirmation modal
  showDeleteModal = signal<boolean>(false);
  listingToDelete = signal<number | null>(null);
  listingToDeleteTitle = signal<string>('');

  // computed
  totalPages = computed(() => {
    const filteredCount = this.filtered().length;
    return Math.max(1, Math.ceil(filteredCount / this.pageSize));
  });

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

  destinations = computed<string[]>(() => {
    const allDestinations = this.listings()
      .map(l => l.destination)
      .filter((dest): dest is string => !!dest);

    return [...new Set(allDestinations)].sort((a, b) => a.localeCompare(b));
  });

  types = computed<string[]>(() => {
    const allTypes = this.listings()
      .map(l => l.type)
      .filter((type): type is string => !!type);

    return [...new Set(allTypes)].sort((a, b) => a.localeCompare(b));
  });

  // Paginated data for current page
  paginatedListings = computed<ListingOverviewVM[]>(() => {
    const filteredData = this.filtered();
    const startIndex = (this.currentPage() - 1) * this.pageSize;
    const endIndex = startIndex + this.pageSize;
    return filteredData.slice(startIndex, endIndex);
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
    const rawDest = this.destination().trim();
    const rawType = this.type().trim();
    const approvedFilter = this.isApproved();
    const maxP = this.maxPrice();
    const minR = this.minRating();

    const q = this.normalize(rawQuery);
    const destNormalized = this.normalize(rawDest);
    const typeNormalized = this.normalize(rawType);

    return data.filter(l => {
      const title = this.normalize(l.title);
      const destinationVal = this.normalize(l.destination);
      const typeVal = this.normalize(l.type);
      const description = this.normalize(l.description ?? '');

      const matchesSearch =
        !q ||
        title.includes(q) ||
        destinationVal.includes(q) ||
        description.includes(q);

      const matchesDestination =
        !destNormalized || destinationVal.includes(destNormalized);

      const matchesType =
        !typeNormalized || typeVal.includes(typeNormalized);

      const matchesApproval =
        approvedFilter === '' ||
        (approvedFilter === 'approved' && l.isApproved === true) ||
        (approvedFilter === 'not-approved' && l.isApproved === false);

      const priceOk =
        (maxP === null || l.pricePerNight <= maxP);

      const ratingOk =
        (minR === null || (l.averageRating ?? 0) >= minR);

      return matchesSearch && matchesDestination && matchesType && matchesApproval && priceOk && ratingOk;
    });
  });

  ngOnInit() {
    this.loadAllListings();
  }

  loadAllListings() {
    this.loading.set(true);
    this.error.set('');

    // Load ALL listings without pagination parameters
    this.listingService.getHostListings().subscribe({
      next: (response) => {
        if (!response.isError) {
          this.listings.set(response.data || []);
          this.totalCount.set(response.data?.length || 0);
          this.currentPage.set(1); // Reset to first page when data loads
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
    const listing = this.listings().find(l => l.id === id);
    if (listing) {
      this.listingToDelete.set(id);
      this.listingToDeleteTitle.set(listing.title || 'this listing');
      this.showDeleteModal.set(true);
    }
  }

  confirmDelete() {
    const id = this.listingToDelete();
    if (!id) return;

    this.listingService.delete(id).subscribe({
      next: (response) => {
        if (!response.isError) {
          this.listings.update(list => list.filter(l => l.id !== id));
        } else {
          this.error.set(response.message || 'Failed to delete listing');
        }
        this.closeDeleteModal();
      },
      error: (err) => {
        this.error.set('Error deleting listing: ' + (err.message || 'Unknown error'));
        this.closeDeleteModal();
      }
    });
  }

  closeDeleteModal() {
    this.showDeleteModal.set(false);
    this.listingToDelete.set(null);
    this.listingToDeleteTitle.set('');
  }

  resetFilters() {
    this.search.set('');
    this.destination.set('');
    this.type.set('');
    this.maxPrice.set(null);
    this.minRating.set(null);
    this.isApproved.set('');
    this.currentPage.set(1); // Reset to first page
  }

  goToPage(page: number) {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
    }
  }

  nextPage() {
    if (this.currentPage() < this.totalPages()) {
      this.currentPage.update(p => p + 1);
    }
  }

  prevPage() {
    if (this.currentPage() > 1) {
      this.currentPage.update(p => p - 1);
    }
  }

  trackById(index: number, item: ListingOverviewVM): number {
    return item.id ?? index;
  }
}