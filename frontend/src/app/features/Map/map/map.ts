import { Component, OnInit, OnDestroy, Inject, PLATFORM_ID } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { Router } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { Subscription } from 'rxjs';
import { MapService } from '../../../core/services/map/map';
import { PropertyMap } from '../../../core/models/map.model';
import { FavoriteButton } from '../../favorites/favorite-button/favorite-button';
import { FavoriteStoreService } from '../../../core/services/favoriteService/favorite-store-service';
import { UserPreferencesService } from '../../../core/services/user-preferences/user-preferences.service';
import { ListingOverviewVM } from '../../../core/models/listing.model';

@Component({
  selector: 'app-map',
  standalone: true,
  imports: [CommonModule, TranslateModule, FavoriteButton],
  templateUrl: './map.html',
  styleUrls: ['./map.css'],
})
export class MapComponent implements OnInit, OnDestroy {
  private map: any;
  private markers: any[] = [];
  properties: PropertyMap[] = [];
  selectedProperty: PropertyMap | null = null;
  isLoading = false;
  private langChangeSubscription?: Subscription;
  isFavorited = false;

  private leaflet: any;
  private customIcon: any;
  constructor(
    private mapService: MapService,
    private router: Router,
    private translate: TranslateService,
    private favoriteStore: FavoriteStoreService,
    private userPreferences: UserPreferencesService,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {}

  async ngOnInit(): Promise<void> {
    if (isPlatformBrowser(this.platformId)) {
      this.leaflet = await import('leaflet');
      // Add some delay to ensure DOM is ready
      setTimeout(() => {
        this.initMap();
      }, 100);

      // Subscribe to language changes and refresh markers
      this.langChangeSubscription = this.translate.onLangChange.subscribe(() => {
        if (this.properties.length > 0) {
          this.updateMarkers();
        }
      });
    }
  }

  ngOnDestroy(): void {
    if (this.langChangeSubscription) {
      this.langChangeSubscription.unsubscribe();
    }
  }

  private initMap(): void {
    try {
      console.log('Initializing map...');
      const mapElement = document.getElementById('map');
      if (!mapElement) {
        console.error('Map container element not found!');
        return;
      }
      // Create a lightweight inline SVG DivIcon to avoid external image requests
      const createSvgPin = (color: string) => `
        <svg width="32" height="42" viewBox="0 0 32 42" xmlns="http://www.w3.org/2000/svg" style="filter: drop-shadow(0 4px 8px rgba(0,0,0,0.3));">
          <path d="M16 0C10.477 0 6 4.477 6 10c0 7.5 10 22 10 22s10-14.5 10-22c0-5.523-4.477-10-10-10z" fill="${color}" stroke="white" stroke-width="2"/>
          <circle cx="16" cy="11" r="4" fill="white"/>
          <text x="16" y="14" text-anchor="middle" fill="${color}" font-size="8" font-weight="bold">$</text>
        </svg>
      `;

      this.map = this.leaflet.map('map', {
        center: [30.0444, 31.2357], // Cairo
        zoom: 12,
      });

      console.log('Map instance created:', this.map);

      this.leaflet.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '© OpenStreetMap contributors'
      }).addTo(this.map);

      console.log('Tile layer added');

      // Update properties when map moves
      this.map.on('moveend', () => {
        console.log('Map moved, reloading properties');
        this.loadProperties();
      });

      // Initial load
      console.log('Loading initial properties');
      this.loadProperties();
    } catch (error) {
      console.error('Error initializing map:', error);
    }
  }

  private loadProperties(): void {
    try {
      if (!this.map) {
        console.warn('Map not initialized yet');
        return;
      }

      this.isLoading = true;
      // On initial load, use global bounds to show all properties
      const params = {
        northEastLat: 90,
        northEastLng: 180,
        southWestLat: -90,
        southWestLng: -180,
      };

      console.log('Loading properties with global bounds:', params);

      this.mapService.getProperties(params).subscribe(
        (res: any) => {
          this.isLoading = false;
          let properties = res.properties || [];

          // Convert to ListingOverviewVM for personalized sorting
          const listings: ListingOverviewVM[] = properties.map((p: PropertyMap) => ({
            id: p.id,
            title: p.title,
            pricePerNight: p.pricePerNight,
            location: p.location || '',
            mainImageUrl: p.mainImageUrl,
            averageRating: p.averageRating,
            reviewCount: p.reviewCount || 0,
            isApproved: true,
            description: p.description || '',
            destination: p.destination || '',
            type: p.type || '',
            bedrooms: p.bedrooms || 0,
            bathrooms: p.bathrooms || 0,
            createdAt: '',
            priority: 0,
            viewCount: 0,
            favoriteCount: 0,
            bookingCount: 0,
            amenities: p.amenities || []
          }));

          // Sort by user preferences
          const sorted = this.userPreferences.sortByRelevance(listings);

          // Convert back to PropertyMap format
          this.properties = sorted.map(l => ({
            id: l.id,
            title: l.title,
            description: l.description,
            pricePerNight: l.pricePerNight,
            location: l.location,
            latitude: 0, // Will be set from original data
            longitude: 0, // Will be set from original data
            mainImageUrl: l.mainImageUrl,
            rating: l.averageRating || 0,
            reviewCount: l.reviewCount,
            destination: l.destination,
            type: l.type,
            bedrooms: l.bedrooms,
            bathrooms: l.bathrooms,
            amenities: l.amenities
          }));

          // Restore lat/lng from original properties
          this.properties.forEach((p, idx) => {
            const original = properties.find((op: PropertyMap) => op.id === p.id);
            if (original) {
              p.latitude = original.latitude;
              p.longitude = original.longitude;
            }
          });

          this.updateMarkers();
        },
        (error: any) => {
          this.isLoading = false;
          console.error('Error loading properties:', error);
          if (error.error && error.error.Message) {
            console.error('Server error:', error.error.Message);
          }
        }
      );
    } catch (error) {
      this.isLoading = false;
      console.error('Error in loadProperties:', error);
    }
  }

  private updateMarkers(): void {
    try {
      // Remove existing markers
      this.markers.forEach((marker) => marker.remove());
      this.markers = [];

      // Add new markers
      this.properties.forEach((p) => {
        try {
          const lat = Number(p.latitude);
          const lng = Number(p.longitude);
          if (!isFinite(lat) || !isFinite(lng)) {
            console.warn(`Property ${p.id} has invalid coordinates:`, p.latitude, p.longitude);
            return;
          }

          // Determine if property is premium based on price
          const isPremium = p.pricePerNight > 200;
          const iconColor = isPremium ? '#f39c12' : '#DC143C';

          // Create dynamic icon
          const dynamicIcon = this.leaflet.divIcon({
            className: 'custom-leaflet-icon',
            html: this.createSvgPin(iconColor),
            iconSize: [32, 42],
            iconAnchor: [16, 42],
            popupAnchor: [0, -40]
          });

          const currency = this.translate.instant('map.currency');
          const perNight = this.translate.instant('map.perNight');
          const reviews = this.translate.instant('map.reviews');
          const noReviews = this.translate.instant('map.noReviews');
          const premiumLabel = this.translate.instant('map.premiumProperty');

          const marker = this.leaflet
            .marker([lat, lng], { icon: dynamicIcon })
            .addTo(this.map)
            .bindPopup(`
              <div style="min-width: 200px;">
                <b>${p.title}</b><br>
                <strong>${p.pricePerNight} ${currency}</strong> ${perNight}<br>
                ${p.averageRating ? `⭐ ${p.averageRating.toFixed(1)} (${p.reviewCount} ${reviews})` : noReviews}<br>
                ${isPremium ? `<span style="color: #f39c12; font-weight: bold;">✨ ${premiumLabel}</span>` : ''}
              </div>
            `);

          marker.on('click', () => {
            this.selectedProperty = p;
            this.checkIfFavorited();
          });

          this.markers.push(marker);
        } catch (error) {
          console.error(`Error creating marker for property ${p.id}:`, error);
        }
      });
    } catch (error) {
      console.error('Error in updateMarkers:', error);
    }
  }

  // Helper method to create SVG pin
  private createSvgPin(color: string): string {
    return `
      <svg width="32" height="42" viewBox="0 0 32 42" xmlns="http://www.w3.org/2000/svg" style="filter: drop-shadow(0 4px 8px rgba(0,0,0,0.3));">
        <path d="M16 0C10.477 0 6 4.477 6 10c0 7.5 10 22 10 22s10-14.5 10-22c0-5.523-4.477-10-10-10z" fill="${color}" stroke="white" stroke-width="2"/>
        <circle cx="16" cy="11" r="4" fill="white"/>
        <text x="16" y="14" text-anchor="middle" fill="${color}" font-size="8" font-weight="bold">$</text>
      </svg>
    `;
  }

  // New methods for enhanced UI functionality
  zoomIn(): void {
    if (this.map) {
      this.map.zoomIn();
    }
  }

  zoomOut(): void {
    if (this.map) {
      this.map.zoomOut();
    }
  }

  resetView(): void {
    if (this.map) {
      this.map.setView([30.0444, 31.2357], 12);
    }
  }

  closePropertyCard(): void {
    this.selectedProperty = null;
  }

  viewPropertyDetails(propertyId: number): void {
    this.router.navigate(['/listings', propertyId]);
  }

  onFavoriteChanged(isFavorited: boolean): void {
    this.isFavorited = isFavorited;
    if (this.selectedProperty) {
      this.favoriteStore.updateFavoriteState(this.selectedProperty.id, isFavorited);
    }
  }

  checkIfFavorited(): void {
    if (this.selectedProperty) {
      this.isFavorited = this.favoriteStore.isFavorited(this.selectedProperty.id);
    }
  }
}
