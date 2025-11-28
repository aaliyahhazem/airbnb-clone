import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ListingOverviewVM } from '../../../core/models/listing.model';

@Component({
  selector: 'app-listing-card',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './listing-card.html',
  styleUrls: ['./listing-card.css']
})
export class ListingCard implements OnInit {
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

  get shortDescription(): string {
    const text = this.listing?.description ?? '';
    return text.length > 25 ? text.slice(0, 25) + '...' : text;
  }

  get price(): number {
    return this.listing?.pricePerNight ?? 0;
  }
}
