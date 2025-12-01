import { Component, Input, OnInit, Output, EventEmitter, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ListingOverviewVM } from '../../../core/models/listing.model';
import { FavoriteButton } from '../../favorites/favorite-button/favorite-button';
import { FavoriteStoreService } from '../../../core/services/favoriteService/favorite-store-service';

@Component({
  selector: 'app-listing-card',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule,FavoriteButton],
  templateUrl: './listing-card.html',
  styleUrls: ['./listing-card.css']
})
export class ListingCard implements OnInit {
  @Input() listing!: ListingOverviewVM;
  @Input() isFavorited = false;
  // Emit a payload containing listing id + new favorite state so parent components can react
  @Output() favoriteChanged = new EventEmitter<{ listingId: number; isFavorited: boolean }>();
  private favoriteStore = inject(FavoriteStoreService);

  ngOnInit(): void {
    if (!this.listing) this.listing = {} as ListingOverviewVM;
    // Check if this listing is favorited
    this.favoriteStore.favorites$.subscribe(() => {
      if (this.listing.id) {
        this.isFavorited = this.favoriteStore.isFavorited(this.listing.id);
      }
    });
  }

  get imageSrc(): string {
    return this.listing?.mainImageUrl || 'assets/images/placeholder-listing.jpg';
  }

  get rating(): number {
    return this.listing?.averageRating ?? 0;
  }

  get shortDescription(): string {
    const text = this.listing?.description ?? '';
    return text.length > 25 ? text.slice(0, 25) + '...' : text;
  }

  get price(): number {
    return this.listing?.pricePerNight ?? 0;
  }
  onFavoriteChanged(isFavorited: boolean): void {
    console.log('Listing card favorite changed:', this.listing.id, isFavorited);
    this.isFavorited = isFavorited;

    // emit structured payload so parent can act on it
    if (this.listing && this.listing.id) {
      this.favoriteChanged.emit({ listingId: this.listing.id, isFavorited });
      // keep optimistic UI in sync
      this.favoriteStore.updateFavoriteState(this.listing.id, isFavorited);
    }
  }
} 
