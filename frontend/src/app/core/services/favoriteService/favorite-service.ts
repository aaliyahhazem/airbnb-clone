import { Injectable } from '@angular/core';
import { BaseService } from '../api/base.service';
import { map, Observable } from 'rxjs';
import { FavoriteListingVM, FavoriteResponse, FavoriteStatsVM, FavoriteVM, PaginatedFavoritesVM } from '../../models/favorite';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root',
})
export class FavoriteService extends BaseService {
  private readonly url = `${this.apiBase}/favorite`;
   constructor(http: HttpClient) {super(http);}
 // Add listing to favorites
addFavorite(listingId: number): Observable<FavoriteResponse<FavoriteVM>> {
  return this.http.post<FavoriteResponse<FavoriteVM>>(this.url, { listingId }).pipe(
    map(res => ({
      ...res,
      result: res.result ?? ({} as FavoriteVM)
    }))
  );
}
    // Remove listing from favorites
  removeFavorite(listingId: number): Observable<FavoriteResponse<boolean>> {
    return this.http.delete<FavoriteResponse<boolean>>(`${this.url}/${listingId}`);
  }
   // Toggle favorite status
  toggleFavorite(listingId: number): Observable<FavoriteResponse<boolean>> {
    return this.http.post<FavoriteResponse<boolean>>(`${this.url}/toggle/${listingId}`, {});
  }
   // Get current user's favorites
  getMyFavorites(): Observable<FavoriteResponse<FavoriteVM[]>> {
    return this.http.get<FavoriteResponse<FavoriteVM[]>>(`${this.url}/me`).pipe(
      map(res => ({
        ...res,
        result: res.result || []
      }))
    );
  }

    // Get paginated favorites
  getMyFavoritesPaginated(page: number = 1, pageSize: number = 10): Observable<FavoriteResponse<PaginatedFavoritesVM>> {
    const params = this.buildParams({ page, pageSize });
    return this.http.get<FavoriteResponse<PaginatedFavoritesVM>>(`${this.url}/me/paginated`, { params });
  }
 // Check if listing is favorited
  checkIsFavorited(listingId: number): Observable<FavoriteResponse<boolean>> {
    return this.http.get<FavoriteResponse<boolean>>(`${this.url}/check/${listingId}`);
  }
  // Get count of user's favorites
  getMyFavoritesCount(): Observable<FavoriteResponse<number>> {
    return this.http.get<FavoriteResponse<number>>(`${this.url}/me/count`);
  }
    // Get count of favorites for a listing
  getListingFavoritesCount(listingId: number): Observable<FavoriteResponse<number>> {
    return this.http.get<FavoriteResponse<number>>(`${this.url}/listing/${listingId}/count`);
  }
  // Get trending listings
  getTrendingListings(count: number = 10): Observable<FavoriteResponse<FavoriteListingVM[]>> {
    const params = this.buildParams({ count });
    return this.http.get<FavoriteResponse<FavoriteListingVM[]>>(`${this.url}/trending`, { params });
  }
  // Clear all favorites
  clearAllFavorites(): Observable<FavoriteResponse<boolean>> {
    return this.http.delete<FavoriteResponse<boolean>>(`${this.url}/me/clear`);
  }
  // Get favorite stats (admin only)
  getFavoriteStats(): Observable<FavoriteResponse<FavoriteStatsVM>> {
    return this.http.get<FavoriteResponse<FavoriteStatsVM>>(`${this.url}/stats`);
  }
}
