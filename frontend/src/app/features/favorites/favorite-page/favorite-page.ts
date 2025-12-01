import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { FavoriteStoreService } from '../../../core/services/favoriteService/favorite-store-service';
import { Router } from '@angular/router';
import { FavoriteListingVM, FavoriteVM } from '../../../core/models/favorite';
import { ListingCard } from '../../listings-page/listing-card/listing-card';
import { ListingOverviewVM } from '../../../core/models/listing.model';

@Component({
  selector: 'app-favorite-page',
  standalone: true,
  imports: [CommonModule, FavoriteButton, TranslateModule],
  templateUrl: './favorite-page.html',
  styleUrl: './favorite-page.css',
})
export class FavoritePage implements OnInit {
  private store = inject(FavoriteStoreService);
  private router = inject(Router);

  favorites: FavoriteVM[] = [];
  listings: ListingOverviewVM[] = [];
  favoriteCount = 0;
  loading = true;
  error = '';

  ngOnInit(): void {
    this.loadFavorites();

    // Subscribe to store updates
    this.store.favorites$.subscribe({
      next: (favorites) => {
        this.favorites = favorites;
        // Convert FavoriteListingVM to ListingOverviewVM format
        this.listings = favorites
          .filter(fav => fav.listing)
          .map(fav => this.convertToListingOverview(fav.listing!));
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load favorites';
        this.loading = false;
      }
    });

    this.store.favoriteCount$.subscribe(cnt => {
      this.favoriteCount = cnt;
      // if we have a non-zero count but no favorites loaded yet, fetch them
      if (cnt > 0 && this.favorites.length === 0) {
        this.loadFavorites();
      }
    });
  }
  trackByListingId(_index: number, item: ListingOverviewVM) {
    return item.id;
  }
  private convertToListingOverview(favListing: FavoriteListingVM): ListingOverviewVM {
    return {
      id: favListing.id,
      title: favListing.title,
      pricePerNight: favListing.pricePerNight,
      location: favListing.location,
      mainImageUrl: this.normalizeImageUrl(favListing.mainImageUrl),
      averageRating: favListing.averageRating || 0,
      reviewCount: favListing.reviewCount,
      isApproved: favListing.isApproved,
      description: favListing.description,
      destination: favListing.destination,
      type: favListing.type,
      bedrooms: favListing.bedrooms,
      bathrooms: favListing.bathrooms
    };
  }
  loadFavorites(): void {
    this.loading = true;
    this.store.loadFavorites();
  }

  removeFavorite(listingId: number, event: Event): void {
    event.stopPropagation();

    if (!confirm('Remove this listing from your favorites?')) return;

    // Optimistic UI update: remove from view immediately, then call API.
    const backupFavorites = [...this.favorites];
    const backupListings = [...this.listings];
    const backupCount = this.favoriteCount;

    // remove locally for instant feedback
    this.favorites = this.favorites.filter(f => f.listingId !== listingId);
    this.listings = this.listings.filter(l => l.id !== listingId);
    this.favoriteCount = Math.max(0, this.favoriteCount - 1);

    // update store's local cache so other parts of the app react quickly
    try { this.store.updateFavoriteState(listingId, false); } catch (e) { /* safe */ }

    this.store.removeFavorite(listingId).subscribe({
      next: () => {
        console.log('Removed from favorites');
      },
      error: (err) => {
        // rollback UI on error
        this.favorites = backupFavorites;
        this.listings = backupListings;
        this.favoriteCount = backupCount;
        this.error = 'Failed to remove favorite';
        console.error(err);
      }
    });
  }

  onFavoriteChanged(listingId: number, isFavorited: boolean): void {
    console.log('Favorite changed:', listingId, isFavorited); // Debug log

    if (!isFavorited) {
      // Immediately remove from local arrays for instant UI update
      this.listings = this.listings.filter(listing => listing.id !== listingId);
      this.favorites = this.favorites.filter(fav => fav.listingId !== listingId);

      // Also update the store to ensure consistency
      this.store.updateFavoriteState(listingId, false);
    }
  }

  isFavorited(listingId: number): boolean {
    return this.store.isFavorited(listingId);
  }

  viewListing(listingId: number): void {
    this.router.navigate(['/host', listingId]);
  }

  goToListings(): void {
    this.router.navigate(['/listings']);
  }

  clearAll(): void {
    if (!confirm('Are you sure you want to clear all favorites?')) return;

    // Optimistic clear: update UI immediately and send API request.
    const backupFavorites = [...this.favorites];
    const backupListings = [...this.listings];
    const backupCount = this.favoriteCount;

    // Clear UI immediately
    this.favorites = [];
    this.listings = [];
    this.favoriteCount = 0;

    // update store cache to reflect optimistic clear
    try { this.store.setOptimisticClearAll(); } catch (e) { /* no-op */ }

    this.store.clearAll().subscribe({
      next: () => {
        console.log('All favorites cleared');
      },
      error: (err) => {
        // rollback on failure
        this.favorites = backupFavorites;
        this.listings = backupListings;
        this.favoriteCount = backupCount;
        // restore authoritative store state
        this.store.loadFavorites();

        this.error = 'Failed to clear favorites';
        console.error(err);
      }
    });
  }

  normalizeImageUrl(url?: string): string {
    if (!url) return 'https://via.placeholder.com/300x200?text=No+Image';
    if (url.startsWith('http://') || url.startsWith('https://')) return url;
    if (url.startsWith('/')) return `http://localhost:5235${url}`;
    return `http://localhost:5235/${url}`;
  }
}
