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
  ngOnInit(): void {
    // Subscribe to store updates for this specific listing
    this.store.favorites$.subscribe(() => {
      this.isFavorited = this.store.isFavorited(this.listingId);
    });
  }
  toggleFavorite(event: Event): void {
    event.stopPropagation();
    event.preventDefault();

    if (this.loading) return;

    this.loading = true;
    const newState = !this.isFavorited;
    this.isFavorited = newState;
    this.favoriteChanged.emit(newState);

    this.store.toggleFavorite(this.listingId).subscribe({
      next: (res) => {
        this.loading = false;
        if (!res.isError && res.result !== undefined) {
          if (res.result !== newState) {
          this.isFavorited = res.result;
            this.favoriteChanged.emit(res.result);
          }
        }
        this.loading = false;
      },
      error: (err) => {
        console.error('Failed to toggle favorite', err);
        this.loading = false;
        this.isFavorited = !newState;
        this.favoriteChanged.emit(!newState);
      }
    });
  }
}
