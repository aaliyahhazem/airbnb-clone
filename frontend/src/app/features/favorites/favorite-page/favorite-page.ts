import { Component, inject, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { ListingCard } from '../../listings-page-user-View/listing-card/listing-card';
import { FavoriteStoreService } from '../../../core/services/favoriteService/favorite-store-service';
import { FavoriteService } from '../../../core/services/favoriteService/favorite-service';
import { Router } from '@angular/router';
import Swal from 'sweetalert2';
import { FavoriteListingVM, FavoriteVM } from '../../../core/models/favorite';
import { ListingOverviewVM } from '../../../core/models/listing.model';
import { ListingService } from '../../../core/services/listings/listing.service';

@Component({
  selector: 'app-favorite-page',
  standalone: true,
  imports: [CommonModule, TranslateModule, ListingCard],
  templateUrl: './favorite-page.html',
  styleUrl: './favorite-page.css',
})
export class FavoritePage implements OnInit {
  private store = inject(FavoriteStoreService);
  private router = inject(Router);
  private api = inject(FavoriteService);
  private favoriteStore = inject(FavoriteStoreService);
  private listingService = inject(ListingService);


  favorites: FavoriteVM[] = [];
  listings: ListingOverviewVM[] = [];
  favoriteCount = 0;
  loading = true;
  error = '';
  listingFavoriteCounts: Map<number, number> = new Map();

  ngOnInit(): void {
    this.favoriteStore.loadFavorites();
    // Subscribe to store updates
    this.store.favorites$.subscribe({
      next: (favorites) => {
        this.favorites = favorites;
        // Convert FavoriteListingVM to ListingOverviewVM format
        this.listings = [];

        favorites.forEach(fav => {
          if (!fav.listing) return;
          const overview = this.convertToListingOverview(fav.listing);
          this.listings.push(overview);
          this.listingService.getById(fav.listingId).subscribe({
            next: (res) => {
              if (!res.isError && res.data) {
                overview.mainImageUrl = res.data.mainImageUrl; 
              }
            },
            error: () => { }
          });
        });
        // Fetch favorite count for each listing
        this.listings.forEach(listing => {
          this.api.getListingFavoritesCount(listing.id).subscribe({
            next: (res) => {
              if (!res.isError && res.result !== undefined) {
                this.listingFavoriteCounts.set(listing.id, res.result);
                // Update the listing's favoriteCount
                listing.favoriteCount = res.result;
              }
            },
            error: (err) => console.warn('Failed to get favorite count for listing', listing.id, err)
          });
        });

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
    // Ensure all required ListingOverviewVM fields are present and provide sensible defaults
    return {
      id: favListing.id,
      title: favListing.title,
      pricePerNight: favListing.pricePerNight ?? 0,
      location: favListing.location ?? '',
      mainImageUrl: this.listingService['normalizeImageUrl'](
        favListing.mainImageUrl?.startsWith('/')
          ? favListing.mainImageUrl
          : `/${favListing.mainImageUrl}`
      ),
      averageRating: favListing.averageRating ?? 0,
      reviewCount: favListing.reviewCount ?? 0,
      isApproved: favListing.isApproved ?? false,
      description: favListing.description ?? '',
      destination: favListing.destination ?? '',
      type: favListing.type ?? '',
      bedrooms: favListing.bedrooms ?? 0,
      bathrooms: favListing.bathrooms ?? 0,
      // ListingOverviewVM requires these additional fields — supply defaults when converting
      createdAt: (favListing as any).createdAt ?? new Date().toISOString(),
      priority: 0,
      viewCount: 0,
      favoriteCount: favListing.favoriteCount ?? 0,
      bookingCount: 0
    };
  }
  loadFavorites(): void {
    this.loading = true;
    this.store.loadFavorites();
  }

  removeFavorite(listingId: number, event: Event): void {
    event.stopPropagation();

    // Find listing title for the alert
    const listing = this.listings.find(l => l.id === listingId);
    const listingTitle = listing?.title || 'this listing';

    Swal.fire({
      title: 'Remove from Favorites?',
      html: `
        <div style="display:flex;align-items:center;gap:12px;">
          <img src="/3.png" alt="hero" style="width:100px;height:100px;border-radius:50%;object-fit:cover;" />
          <div style="text-align:left;">
            <div style="font-weight:600;font-size:0.95rem;">Are you sure?</div>
            <div style="color:#6c757d;margin-top:6px;font-size:0.9rem;">You are about to remove <strong>${listingTitle}</strong> from your favorites.</div>
          </div>
        </div>
      `,
      showCancelButton: true,
      cancelButtonText: 'Keep it',
      confirmButtonText: 'Yes, remove',
      icon: 'warning',
      customClass: {
        popup: 'swal-custom-popup',
        confirmButton: 'btn btn-danger',
        cancelButton: 'btn btn-outline-secondary'
      },
      buttonsStyling: false,
      reverseButtons: true
    }).then(res => {
      if (!res.isConfirmed) return;

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
          Swal.fire({
            title: 'Removed!',
            text: `${listingTitle} was removed from your favorites.`,
            icon: 'success',
            showConfirmButton: false,
            timer: 1600,
            toast: true,
            position: 'top-end'
          });
        },
        error: (err) => {
          // rollback UI on error
          this.favorites = backupFavorites;
          this.listings = backupListings;
          this.favoriteCount = backupCount;
          this.error = 'Failed to remove favorite';
          Swal.fire({
            title: 'Error',
            text: 'Could not remove from favorites. Please try again.',
            icon: 'error',
            customClass: { popup: 'swal-custom-popup' }
          });
          console.error(err);
        }
      });
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

  async clearAll(): Promise<void> {
    // Use SweetAlert2 modal to confirm clearing favorites — branded with our app identity.
    const res = await Swal.fire({
      title: 'Clear all favorites?',
      html: `
        <div style="display:flex;align-items:center;gap:12px;">
          <img src="/3.png" alt="hero" style="width:120px;height:120px;border-radius:50%;object-fit:cover;" />
          <div style="text-align:left;">
            <div style="font-weight:600;font-size:1.05rem;">Are you sure?</div>
            <div style="color:#6c757d;margin-top:6px">This will remove all saved listings from your favorites. You can always add them again later.</div>
          </div>
        </div>
      `,
      showCancelButton: true,
      cancelButtonText: 'Cancel',
      confirmButtonText: 'Yes, clear all',
      icon: 'warning',
      customClass: {
        popup: 'swal-custom-popup',
        confirmButton: 'btn btn-danger',
        cancelButton: 'btn btn-outline-secondary'
      },
      buttonsStyling: false,
      reverseButtons: true
    });

    if (!res.isConfirmed) return;

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
        // Friendly confirmation
        Swal.fire({
          title: 'All favorites cleared',
          text: 'All your saved listings were removed from favorites.',
          icon: 'success',
          showConfirmButton: false,
          timer: 1600,
          toast: true,
          position: 'top-end'
        });
      },
      error: (err) => {
        // rollback on failure
        this.favorites = backupFavorites;
        this.listings = backupListings;
        this.favoriteCount = backupCount;
        // restore authoritative store state
        this.store.loadFavorites();

        this.error = 'Failed to clear favorites';
        Swal.fire({
          title: 'Could not clear favorites',
          text: this.error,
          icon: 'error',
          confirmButtonText: 'OK',
          customClass: { popup: 'swal-custom-popup' }
        });
        console.error(err);
      }
    });
  }
}
