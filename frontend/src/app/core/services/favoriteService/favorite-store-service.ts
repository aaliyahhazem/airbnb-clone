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
            this.favoriteListingIds.add(listingId);
            this.loadFavorites(); // Reload to get complete data
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
            const current = this.favoritesSubject.value;
            this.favoritesSubject.next(current.filter(f => f.listingId !== listingId));
            this.favoriteCountSubject.next(this.favoriteCountSubject.value - 1);
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
          if (!res.isError) {
            if (res.result) {
              this.favoriteListingIds.add(listingId);
            } else {
              this.favoriteListingIds.delete(listingId);
            }
            this.loadFavorites();
          }
        }
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
}