import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FavoriteStoreService } from '../../../core/services/favoriteService/favorite-store-service';
import { Router, RouterLink } from '@angular/router';
import { FavoriteListingVM, FavoriteVM } from '../../../core/models/favorite';
import { FavoriteButton } from "../favorite-button/favorite-button";

@Component({
  selector: 'app-favorite-page',
  imports: [CommonModule, RouterLink, FavoriteButton],
  templateUrl: './favorite-page.html',
  styleUrl: './favorite-page.css',
})
export class FavoritePage implements OnInit {
  private store = inject(FavoriteStoreService);
  private router = inject(Router);

  favorites: FavoriteVM[] = [];
  listings: FavoriteListingVM[] = [];
  loading = true;
  error = '';

  ngOnInit(): void {
    this.loadFavorites();
    
    // Subscribe to store updates
    this.store.favorites$.subscribe({
      next: (favorites) => {
        this.favorites = favorites;
        // Extract listings from favorites
        this.listings = favorites
          .filter(fav => fav.listing) // Only include favorites with listing data
          .map(fav => fav.listing!);
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load favorites';
        this.loading = false;
      }
    });
  }

  loadFavorites(): void {
    this.loading = true;
    this.store.loadFavorites();
  }

  removeFavorite(listingId: number, event: Event): void {
    event.stopPropagation();
    
    if (!confirm('Remove this listing from your favorites?')) return;

    this.store.removeFavorite(listingId).subscribe({
      next: () => {
        console.log('Removed from favorites');
      },
      error: (err) => {
        this.error = 'Failed to remove favorite';
        console.error(err);
      }
    });
  }

  onFavoriteChanged(listingId: number, isFavorited: boolean): void {
    // Refresh favorites list if unfavorited
    if (!isFavorited) {
      this.loadFavorites();
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

    this.store.clearAll().subscribe({
      next: () => {
        console.log('All favorites cleared');
      },
      error: (err) => {
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