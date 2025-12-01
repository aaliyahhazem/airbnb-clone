import { Component, OnInit, OnDestroy, inject, ChangeDetectorRef, PLATFORM_ID } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ListingService } from '../../../core/services/listings/listing.service';
import { ListingDetailVM } from '../../../core/models/listing.model';
import { Subscription } from 'rxjs';
import { CreateBooking } from "../../booking/create-booking/create-booking";

@Component({
  selector: 'app-listings-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule],
  templateUrl: './listings-detail.html',
  styleUrls: ['./listings-detail.css'],
})
export class ListingsDetail implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private listingService = inject(ListingService);
  private cdr = inject(ChangeDetectorRef);
  private platformId = inject(PLATFORM_ID);
  private sub: Subscription | null = null;

  listing?: ListingDetailVM;
  loading = true;
  error = '';
  currentImageIndex = 0;
  canEdit = false; // Add edit permission property

  //Bookings Props 
  showBookingForm = false;
  bookingSuccess = false;
  currentBooking: any = null;

  private leaflet: any;
  private detailMap: any | null = null;

  ngOnInit(): void {
    // Subscribe to route params so component reloads when navigated via routerLink
    this.sub = this.route.paramMap.subscribe((params) => {
      this.loading = true;
      this.error = '';
      this.currentImageIndex = 0;
      const idParam = params.get('id');
      if (idParam) {
        this.loadListing(+idParam);
      } else {
        this.error = 'Listing ID not provided';
        this.loading = false;
      }
    });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
    if (this.detailMap) {
      try { this.detailMap.remove(); } catch { /* ignore */ }
      this.detailMap = null;
    }
    // Clear Leaflet to free memory
    this.leaflet = null;
  }

  private loadListing(id: number): void {
    this.listingService.getById(id).subscribe(
      (response) => {
        if (!response.isError && response.data) {
          this.listing = response.data;
          this.loading = false;
          // Detect changes first to render content
          try { this.cdr.detectChanges(); } catch { /* ignore */ }
          // Then initialize map asynchronously without blocking UI
          void this.initDetailMap();
        } else {
          this.error = response.message || 'Failed to load listing';
          this.loading = false;
          try { this.cdr.detectChanges(); } catch { /* ignore */ }
        }
      },
      (err) => {
        this.error = 'Error loading listing details';
        this.loading = false;
        try { this.cdr.detectChanges(); } catch { /* ignore */ }
      }
    );
  }

  private async initDetailMap(): Promise<void> {
    try {
      if (!isPlatformBrowser(this.platformId)) return;
      if (!this.listing) return;

      // Delay map initialization to avoid blocking main thread
      await new Promise(resolve => setTimeout(resolve, 300));

      // Only import if map element exists
      const el = document.getElementById('detail-map');
      if (!el) return;

      this.leaflet = await import('leaflet');

      // remove previous map if exists
      if (this.detailMap) {
        try { this.detailMap.remove(); } catch { /* ignore */ }
        this.detailMap = null;
      }

      const lat = Number(this.listing.latitude) || 0;
      const lng = Number(this.listing.longitude) || 0;

      this.detailMap = this.leaflet.map(el, { 
        center: [lat, lng], 
        zoom: 14, 
        scrollWheelZoom: false,
        // Performance improvements
        preferCanvas: true,
        zoomAnimation: false
      });

      // Use an inline SVG DivIcon so we don't rely on external image files
      const svgPin = `
        <svg width="30" height="42" viewBox="0 0 30 42" xmlns="http://www.w3.org/2000/svg">
          <path d="M15 0C9.477 0 5 4.477 5 10c0 7.5 10 22 10 22s10-14.5 10-22c0-5.523-4.477-10-10-10z" fill="#d00"/>
          <circle cx="15" cy="11" r="4" fill="#fff"/>
        </svg>
      `;
      const customIcon = this.leaflet.divIcon({
        className: 'custom-leaflet-icon',
        html: svgPin,
        iconSize: [30, 42],
        iconAnchor: [15, 42],
        popupAnchor: [0, -40]
      });

      this.leaflet.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '© OpenStreetMap contributors'
      }).addTo(this.detailMap);

      const marker = this.leaflet.marker([lat, lng], { icon: customIcon }).addTo(this.detailMap);

      // Use data already fetched from listing service instead of making extra API call
      const price = this.listing.pricePerNight ? `<div>${this.listing.pricePerNight} EGP/night</div>` : '';
      const desc = this.listing.description ? `<div style="margin-top:6px;"><small>${this.listing.description}</small></div>` : '';
      const popupHtml = `<b>${this.listing.title}</b>${price}${desc}`;
      marker.bindPopup(popupHtml).openPopup();
    } catch (err) {
      // Map initialization failed; keep UI usable. Debug log commented out.
      // try { console.warn('Detail map init failed', err); } catch {}
    }
  }

  nextImage(): void {
    if (this.listing?.images) {
      this.currentImageIndex = (this.currentImageIndex + 1) % this.listing.images.length;
    }
  }

  prevImage(): void {
    if (this.listing?.images) {
      this.currentImageIndex =
        (this.currentImageIndex - 1 + this.listing.images.length) % this.listing.images.length;
    }
  }

  selectImage(index: number): void {
    this.currentImageIndex = index;
  }

  goBack(): void {
    this.router.navigate(['/host']);
  }

  editListing(): void {
    if (this.listing) {
      this.router.navigate(['/host', this.listing.id, 'edit']);
    }
  }

  //Bookings Methods
  toggleBookingForm(): void {
    this.showBookingForm = !this.showBookingForm;
    this.bookingSuccess = false;
  }

  onBookingCreated(booking: any): void {
    this.bookingSuccess = true;
    this.currentBooking = booking;
    this.showBookingForm = false;
    // The CreateBooking component will navigate to the payment flow (with payment intent),
    // so we simply show success state here and avoid duplicating navigation.
  }

  onBookingCancelled(): void {
    this.showBookingForm = false;
  }
}

