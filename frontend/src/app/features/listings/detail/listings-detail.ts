import { Component, OnInit, OnDestroy, inject, ChangeDetectorRef, PLATFORM_ID } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ListingService } from '../../../core/services/listings/listing.service';
import { MapService } from '../../../core/services/map/map';
import { ListingDetailVM } from '../../../core/models/listing.model';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-listings-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './listings-detail.html',
  styleUrls: ['./listings-detail.css'],
})
export class ListingsDetail implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private listingService = inject(ListingService);
  private cdr = inject(ChangeDetectorRef);
  private platformId = inject(PLATFORM_ID);
  private mapService = inject(MapService);
  private sub: Subscription | null = null;

  listing?: ListingDetailVM;
  loading = true;
  error = '';
  currentImageIndex = 0;

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
  }

  private loadListing(id: number): void {
    this.listingService.getById(id).subscribe(
      (response) => {
        if (!response.isError && response.data) {
          this.listing = response.data;
          // initialize small detail map at the listing coordinates
          void this.initDetailMap();
        } else {
          this.error = response.message || 'Failed to load listing';
        }
        this.loading = false;
        // ensure view updates after async data arrival
        try { this.cdr.detectChanges(); } catch { /* ignore */ }
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

      this.leaflet = await import('leaflet');

      // remove previous map if exists
      if (this.detailMap) {
        try { this.detailMap.remove(); } catch { /* ignore */ }
        this.detailMap = null;
      }

      const el = document.getElementById('detail-map');
      if (!el) return;

      const lat = Number(this.listing.latitude) || 0;
      const lng = Number(this.listing.longitude) || 0;

      this.detailMap = this.leaflet.map(el, { center: [lat, lng], zoom: 14, scrollWheelZoom: false });

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

      // Try to get richer popup content from backend Map API
      if (this.listing && this.listing.id) {
          try {
          this.mapService.getProperty(this.listing.id).subscribe((p: any) => {
            const price = p.pricePerNight ? `<div>${p.pricePerNight} EGP/night</div>` : '';
            const desc = p.description ? `<div style="margin-top:6px;"><small>${p.description}</small></div>` : '';
            const popupHtml = `<b>${p.title || this.listing!.title}</b>${price}${desc}`;
            marker.bindPopup(popupHtml).openPopup();
          }, () => {
            const fallback = `<b>${this.listing!.title}</b><div>${lat.toFixed(6)}, ${lng.toFixed(6)}</div>`;
            marker.bindPopup(fallback).openPopup();
          });
        } catch {
          marker.bindPopup(`<b>${this.listing!.title}</b><br/>${lat.toFixed(6)}, ${lng.toFixed(6)}`).openPopup();
        }
      } else {
        marker.bindPopup(`<b>${this.listing?.title}</b><br/>${lat.toFixed(6)}, ${lng.toFixed(6)}`).openPopup();
      }
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
}

