import { Component, OnInit, ChangeDetectorRef, signal, computed } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ListingOverviewVM } from '../../../core/models/listing.model';
import { ListingService } from '../../../core/services/listings/listing.service';
import { ListingCard } from '../listing-card/listing-card';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';

@Component({
  selector: 'app-listings',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, ListingCard, ReactiveFormsModule, TranslateModule],
  templateUrl: './listings.html',
  styleUrls: ['./listings.css']
})
export class Listings implements OnInit {
  // raw data
  listings = signal<ListingOverviewVM[]>([]);
  totalCount = signal<number>(0);
  loading = signal<boolean>(false);
  error = signal<string>('');

  // pagination
  currentPage = signal<number>(1);
  pageSize = signal<number>(12);
  
  // filters (signals)
  search = signal<string>('');
  location = signal<string>('');
  maxPrice = signal<number | null>(null);
  minRating = signal<number | null>(null);

  // amenities (form)
  form: FormGroup;

  constructor(
    private listingService: ListingService,
    private router: Router,
    private cdr: ChangeDetectorRef,
    private fb: FormBuilder
  ) {
    this.form = this.fb.group({
      amenities: [[]]
    });
  }

  // list of amenities
  amenitiesList = [
    'Wi-Fi', 'Pool', 'Air Conditioning', 'Kitchen',
    'Washer', 'Dryer', 'TV', 'Heating', 'Parking',
    // 'Pet Friendly', 'Gym', 'Hot Tub', 'Fireplace', 'Breakfast',
    // 'Elevator', 'Wheelchair Accessible', 'Garden', 'Balcony', 'Sauna'
  ];

  toggleAmenity(amenity: string): void {
    const currentValue = this.form.get('amenities')?.value || [];
    const amenities = Array.isArray(currentValue) ? [...currentValue] : [];
    const index = amenities.indexOf(amenity);

    if (index > -1) amenities.splice(index, 1);
    else amenities.push(amenity);

    this.form.patchValue({ amenities });
  }

  isAmenitySelected(amenity: string): boolean {
    const amenities: string[] = this.form.get('amenities')?.value || [];
    return amenities.includes(amenity);
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

  // computed list of unique locations
  locations = computed(() => {
    const data = this.listings();
    const set = new Set<string>();
    data.forEach(l => set.add(l.location));
    return Array.from(set);
  });

  // computed filtered list (client-side filtering after server load)
  filtered = computed<ListingOverviewVM[]>(() => {
    const data = this.listings();
    if (!data || !Array.isArray(data)) return [];

    const rawQuery = this.search().trim();
    const rawLoc = this.location().trim();
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

      const priceOk = maxP === null || l.pricePerNight <= maxP;

      const ratingOk =
        minR === null || (l.averageRating ?? 0) >= minR;

      return matchesSearch && matchesLocation && priceOk && ratingOk;
    });
  });

  // computed pagination values
  totalPages = computed(() => {
    const total = this.totalCount();
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
    this.loadListings();

    // refresh on navigation
    this.router.events.subscribe(event => {
      if (event instanceof NavigationEnd && this.router.url.startsWith('/listings')) {
        this.loadListings();
      }
    });
  }

  loadListings(): void {
    this.loading.set(true);
    this.error.set('');

    this.listingService.getPaged(this.currentPage(), this.pageSize()).subscribe({
      next: (res) => {
        console.log('Listings Response:', res);
        this.listings.set(res.data || []);
        const total = res.totalCount || res.data?.length || 0;
        this.totalCount.set(total);
        console.log('Total count set to:', total);
        this.loading.set(false);
        this.cdr.markForCheck();
        window.scrollTo({ top: 0, behavior: 'smooth' });
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
    this.loadListings();
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

  resetFilters() {
    this.search.set('');
    this.location.set('');
    this.maxPrice.set(null);
    this.minRating.set(null);
    this.form.patchValue({ amenities: [] });
    this.currentPage.set(1);
    this.loadListings();
  }

  onDelete(id: number) {
    if (!confirm('Delete this listing?')) return;

    this.listingService.delete(id).subscribe({
      next: () => {
        this.listings.update(list => list.filter(l => l.id !== id));
      },
      error: () => {
        this.error.set('Failed to delete listing');
      }
    });
  }

  trackById(index: number, item: ListingOverviewVM): number {
    return item.id ?? index;
  }
}
