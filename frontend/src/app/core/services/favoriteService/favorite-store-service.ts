import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { FavoriteVM } from '../../models/favorite';
import { FavoriteService } from './favorite-service';

@Injectable({
  providedIn: 'root',
})
export class FavoriteStoreService {
  private favoritesSubject = new BehaviorSubject<FavoriteVM[]>([]);
  public favorites$ = this.favoritesSubject.asObservable();

  private favoriteCountSubject = new BehaviorSubject<number>(0);
  public favoriteCount$ = this.favoriteCountSubject.asObservable();

  // Cache of listing IDs that are favorited (for quick lookups)
  private favoriteListingIds = new Set<number>();
  constructor(private api: FavoriteService) { }

  // Load user's favorites
  loadFavorites(): void {
    this.api.getMyFavorites().subscribe({
      next: (res) => {
        if (!res.isError && res.result) {
          this.favoritesSubject.next(res.result);
          this.favoriteCountSubject.next(res.result.length);

          // Update cache
          this.favoriteListingIds.clear();
          res.result.forEach(fav => this.favoriteListingIds.add(fav.listingId));
        }
      },
      error: (err) => console.error('Failed to load favorites', err)
    });
  }

  // Add to favorites
  addFavorite(listingId: number): Observable<any> {
    return this.api.addFavorite(listingId).pipe(
      tap({
        next: (res) => {
          if (!res.isError) {
            // Update id cache quickly
            this.favoriteListingIds.add(listingId);
            this.favoriteCountSubject.next(this.favoriteCountSubject.value + 1);
            // If server returned the created favorite object, append it to the list
            if (res.result) {
              const current = this.favoritesSubject.value;
              this.favoritesSubject.next([...current, res.result]);
            }
          }
        }
      })
    );
  }
  // Remove from favorites
  removeFavorite(listingId: number): Observable<any> {
    return this.api.removeFavorite(listingId).pipe(
      tap({
        next: (res) => {
          if (!res.isError) {
            this.favoriteListingIds.delete(listingId);
            const currentFavorites = this.favoritesSubject.value;
            const updatedFavorites = currentFavorites.filter(f => f.listingId !== listingId);
            this.favoritesSubject.next(updatedFavorites);

            // Update count immediately
            this.favoriteCountSubject.next(updatedFavorites.length);
          }
        }
      })
    );
  }
  // Toggle favorite
  toggleFavorite(listingId: number): Observable<any> {
    return this.api.toggleFavorite(listingId).pipe(
      tap({
        next: (res) => {
          if (!res.isError && res.result !== undefined) {
            // Apply server result to our local state; no full reload here to avoid
            // overwriting optimistic UI changes. The favorites list can be synced
            // explicitly elsewhere if needed.
            this.updateFavoriteState(listingId, res.result);
            // If the server indicates the listing is now favorited, refresh
            // the full favorites list so the favorites Subject contains
            // the listing objects (required by Favorites page UI).
            // This keeps the Favorites page in sync after toggles.
            if (res.result === true) {
              try { this.loadFavorites(); } catch { /* best-effort */ }
            }
            // Consider whether you want to call loadFavorites() here
            // this.loadFavorites();
          }
        },
        error: (err) => console.error('Failed to toggle favorite', err)
      })
    );
  }
  // Check if listing is favorited (from cache)
  isFavorited(listingId: number): boolean {
    return this.favoriteListingIds.has(listingId);
  }
  // Clear all favorites
  clearAll(): Observable<any> {
    return this.api.clearAllFavorites().pipe(
      tap({
        next: (res) => {
          if (!res.isError) {
            this.favoritesSubject.next([]);
            this.favoriteCountSubject.next(0);
            this.favoriteListingIds.clear();
          }
        }
      })
    );
  }
  // Apply an optimistic clear locally (UI-level only) - does not call the API
  setOptimisticClearAll(): void {
    this.favoritesSubject.next([]);
    this.favoriteCountSubject.next(0);
    this.favoriteListingIds.clear();
  }
  updateFavoriteState(listingId: number, isFavorited: boolean): void {
    if (isFavorited) {
      // Mark id as favorited and increment count
      if (!this.favoriteListingIds.has(listingId)) {
        this.favoriteListingIds.add(listingId);
        this.favoriteCountSubject.next(this.favoriteCountSubject.value + 1);
      }
    } else {
      if (this.favoriteListingIds.has(listingId)) {
        this.favoriteListingIds.delete(listingId);
      }
      const currentFavorites = this.favoritesSubject.value;
      const updatedFavorites = currentFavorites.filter(f => f.listingId !== listingId);

      this.favoritesSubject.next(updatedFavorites);
      this.favoriteCountSubject.next(updatedFavorites.length);
    }
  }
}