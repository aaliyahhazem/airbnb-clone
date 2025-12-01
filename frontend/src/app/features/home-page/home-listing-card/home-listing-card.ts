import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ListingOverviewVM } from '../../../core/models/listing.model';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-home-listing-card',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule],
  templateUrl: './home-listing-card.html',
  styleUrl: './home-listing-card.css',
})
export class HomeListingCard implements OnInit {
  @Input() listing!: ListingOverviewVM;

  ngOnInit(): void {
    if (!this.listing) this.listing = {} as ListingOverviewVM;
  }

  get imageSrc(): string {
    return this.listing?.mainImageUrl || 'assets/images/placeholder-listing.jpg';
  }

  get rating(): number {
    return this.listing?.averageRating ?? 0;
  }

  get price(): number {
    return this.listing?.pricePerNight ?? 0;
  }

  // Priority Badge Logic
  get priorityBadge(): { label: string; emoji: string; class: string } | null {
    const priority = this.listing?.priority ?? 0;

    if (priority >= 100) {
      return { label: 'Top Pick', emoji: '🔥', class: 'badge-top-pick' };
    } else if (priority >= 50) {
      return { label: 'Trending', emoji: '⭐', class: 'badge-trending' };
    } else if (priority >= 20) {
      return { label: 'Popular', emoji: '✨', class: 'badge-popular' };
    }

    return null;
  }

  get hasPriorityBadge(): boolean {
    return this.priorityBadge !== null;
  }
}
