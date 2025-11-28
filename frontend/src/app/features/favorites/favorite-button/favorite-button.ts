import { Component, Input, Output, EventEmitter, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FavoriteStoreService } from '../../../core/services/favoriteService/favorite-store-service';

@Component({
  selector: 'app-favorite-button',
  imports: [],
  templateUrl: './favorite-button.html',
  styleUrl: './favorite-button.css',
})
export class FavoriteButton {
  @Input() listingId!: number;
  @Input() isFavorited = false;
  @Output() favoriteChanged = new EventEmitter<boolean>();

  loading = false;
  private store = inject(FavoriteStoreService);

  toggleFavorite(event: Event): void {
    event.stopPropagation();
    event.preventDefault();

    if (this.loading) return;

    this.loading = true;
    this.store.toggleFavorite(this.listingId).subscribe({
      next: (res) => {
        if (!res.isError && res.result !== undefined) {
          this.isFavorited = res.result;
          this.favoriteChanged.emit(this.isFavorited);
        }
        this.loading = false;
      },
      error: (err) => {
        console.error('Failed to toggle favorite', err);
        this.loading = false;
      }
    });
  }
}
