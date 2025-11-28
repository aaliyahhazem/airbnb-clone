import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ListingOverviewVM } from '../../../core/models/listing.model';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-home-listing-card',
  standalone: true,
  imports: [CommonModule, RouterLink],
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

  // get shortDescription(): string {
  //   const text = this.listing?.description ?? '';
  //   return text.length > 60 ? text.slice(0, 60) + '...' : text;
  // }

  get price(): number {
    return this.listing?.pricePerNight ?? 0;
  }
}
