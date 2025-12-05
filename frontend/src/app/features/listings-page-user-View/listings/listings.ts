import { Component, OnInit, ChangeDetectorRef, signal, computed } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ListingOverviewVM } from '../../../core/models/listing.model';
import { ListingService } from '../../../core/services/listings/listing.service';
import { ListingCard } from '../listing-card/listing-card';
import { FavoriteStoreService } from '../../../core/services/favoriteService/favorite-store-service';
import { UserPreferencesService } from '../../../core/services/user-preferences/user-preferences.service';
import { PersonalizationBadge } from '../../../shared/components/personalization-badge/personalization-badge';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-listings',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, ListingCard, ReactiveFormsModule, TranslateModule, PersonalizationBadge],
  templateUrl: './listings.html',
  styleUrls: ['./listings.css']
})
export class Listings implements OnInit {
  // raw data - all listings loaded at once
  allListings = signal<ListingOverviewVM[]>([]);
  loading = signal<boolean>(false);
  error = signal<string>('');

  // pagination for filtered results
  currentPage = signal<number>(1);
  pageSize = signal<number>(12);

  // filters (signals)
  search = signal<string>('');
  destination = signal<string>('');  // Changed from location to destination
  type = signal<string>('');
  maxPrice = signal<number | null>(null);
  minRating = signal<number | null>(null);
  amenities = signal<string[]>([]);

  constructor(
    private listingService: ListingService,
    private router: Router,
    private cdr: ChangeDetectorRef,
    public favoriteStore: FavoriteStoreService,
    private userPreferences: UserPreferencesService
  ) {}

  onListingFavoriteChanged(payload: { listingId: number; isFavorited: boolean }) {
    try {
      const idx = this.allListings().findIndex(l => l.id === payload.listingId);
      if (idx >= 0) {
        const copy = [...this.allListings()];
        (copy[idx] as any).isFavorited = payload.isFavorited;
        this.allListings.set(copy);

        // Track favorite in user preferences (if favorited)
        if (payload.isFavorited) {
          this.userPreferences.trackFavorite(copy[idx]);
        }
      }
    } catch (e) { console.warn('Failed to update listing favorite state in parent', e); }
  }

  // list of amenities
  amenitiesList = [
    'Wi-Fi', 'Pool', 'Air Conditioning', 'Kitchen',
    'Washer', 'Dryer', 'TV', 'Heating', 'Parking'
  ];

  toggleAmenity(amenity: string): void {
    const currentAmenities = [...this.amenities()];
    const index = currentAmenities.indexOf(amenity);

    if (index > -1) {
      currentAmenities.splice(index, 1);
    } else {
      currentAmenities.push(amenity);
    }

    this.amenities.set(currentAmenities);

    // Track amenity filter in user preferences
    if (currentAmenities.length > 0) {
      this.userPreferences.trackAmenityFilter(currentAmenities);
    }

    // Reset to first page when filters change
    this.currentPage.set(1);
  }

  isAmenitySelected(amenity: string): boolean {
    return this.amenities().includes(amenity);
  }

  // arabic/english normalization
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

  // computed list of unique destinations from all data
  destinations = computed(() => {
    const data = this.allListings();
    const set = new Set<string>();
    data.forEach(l => {
      if (l.destination && l.destination.trim()) {
        set.add(l.destination);
      }
    });
    return Array.from(set).sort((a, b) => a.localeCompare(b));
  });

  // computed list of unique types from all data
  types = computed(() => {
    const data = this.allListings();
    const set = new Set<string>();
    data.forEach(l => {
      if (l.type && l.type.trim()) {
        set.add(l.type);
      }
    });
    return Array.from(set).sort((a, b) => a.localeCompare(b));
  });

  // computed filtered list (client-side filtering on ALL data)
  filtered = computed<ListingOverviewVM[]>(() => {
    const data = this.allListings();
    if (!data || !Array.isArray(data)) return [];

    const rawQuery = this.search().trim();
    const rawDest = this.destination().trim();
    const rawType = this.type().trim();
    const maxP = this.maxPrice();
    const minR = this.minRating();
    const selectedAmenities = this.amenities();

    const q = this.normalize(rawQuery);
    const destNormalized = this.normalize(rawDest);
    const typeNormalized = this.normalize(rawType);

    const filtered = data.filter(l => {
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

      const priceOk = maxP === null || l.pricePerNight <= maxP;

      const ratingOk =
        minR === null || (l.averageRating ?? 0) >= minR;

      // Amenities filter - case-insensitive comparison
      const amenitiesOk = selectedAmenities.length === 0 ||
        selectedAmenities.every((amenity: string) => {
          const normalizedAmenity = amenity.toLowerCase().replace(/[^a-z0-9]/g, '');
          return l.amenities && l.amenities.some((listingAmenity: string) => {
            const normalizedListingAmenity = listingAmenity.toLowerCase().replace(/[^a-z0-9]/g, '');
            return normalizedListingAmenity === normalizedAmenity ||
                   normalizedListingAmenity.includes(normalizedAmenity) ||
                   normalizedAmenity.includes(normalizedListingAmenity);
          });
        });

      return matchesSearch && matchesDestination && matchesType && priceOk && ratingOk && amenitiesOk;
    });

    // Apply personalized sorting based on user preferences
    return this.userPreferences.sortByRelevance(filtered);
  });

  // computed paginated results from filtered data
  paginatedListings = computed(() => {
    const filteredData = this.filtered();
    const pageSize = this.pageSize();
    const currentPage = this.currentPage();
    const startIndex = (currentPage - 1) * pageSize;
    const endIndex = startIndex + pageSize;

    return filteredData.slice(startIndex, endIndex);
  });

  // computed pagination values based on filtered results
  totalFilteredCount = computed(() => this.filtered().length);

  totalPages = computed(() => {
    const total = this.totalFilteredCount();
    const pageSize = this.pageSize();
    return total === 0 ? 1 : Math.ceil(total / pageSize);
  });

  paginationPages = computed(() => {
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

      const start = Math.max(2, current - 1);
      const end = Math.min(total - 1, current + 1);

      for (let i = start; i <= end; i++) {
        pages.push(i);
      }

      if (current < total - 2) pages.push('...');
      pages.push(total);
    }

    return pages;
  });

  ngOnInit(): void {
    this.loadAllListings();

    // refresh on navigation
    this.router.events.subscribe(event => {
      if (event instanceof NavigationEnd && this.router.url.startsWith('/listings')) {
        this.loadAllListings();
      }
    });
  }

  viewOnMap() {
    this.router.navigate(['/map']);
  }

  loadAllListings(): void {
    this.loading.set(true);
    this.error.set('');

    // Load listings with proper pagination (backend max is 100 per page)
    this.listingService.getPaged(1, 100).subscribe({
      next: (res) => {
        console.log('All Listings Response:', res);
        this.allListings.set(res.data || []);
        console.log('Total listings loaded:', res.data?.length || 0);
        this.loading.set(false);
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Error loading listings:', err);
        this.error.set('Failed to load listings');
        this.loading.set(false);
        this.cdr.markForCheck();
      }
    });
  }

  goToPage(page: number | string): void {
    if (typeof page === 'string') return;
    if (page < 1 || page > this.totalPages()) return;

    this.currentPage.set(page);
    // No need to reload data, just update the page
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  nextPage(): void {
    const totalPages = this.totalPages();
    const next = this.currentPage() + 1;
    if (next <= totalPages) {
      this.goToPage(next);
    }
  }

  prevPage(): void {
    const prev = this.currentPage() - 1;
    if (prev >= 1) {
      this.goToPage(prev);
    }
  }

  // Reset to first page when filters change
  onSearchChange(): void {
    this.currentPage.set(1);
    // Track search interaction if there's a query
    const query = this.search().trim();
    if (query.length > 0) {
      // Track first few results as "searched for"
      const topResults = this.filtered().slice(0, 3);
      topResults.forEach(listing => {
        this.userPreferences.trackListingInteraction(listing, 0.5);
      });
    }
  }

  onDestinationChange(): void {
    this.currentPage.set(1);
  }

  onTypeChange(): void {
    this.currentPage.set(1);
  }

  onPriceChange(): void {
    this.currentPage.set(1);
  }

  onRatingChange(): void {
    this.currentPage.set(1);
  }

  resetFilters() {
    this.search.set('');
    this.destination.set('');
    this.type.set('');
    this.maxPrice.set(null);
    this.minRating.set(null);
    this.amenities.set([]);
    this.currentPage.set(1);
    // No need to reload data, filters are applied client-side
  }

  // Get relevance score for a specific listing (0-100)
  getRelevanceScore(listing: ListingOverviewVM): number {
    return this.userPreferences.calculateRelevanceScore(listing);
  }

  trackById(index: number, item: ListingOverviewVM): number {
    return item.id ?? index;
  }
}
