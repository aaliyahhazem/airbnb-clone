import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ListingService } from '../../../core/services/listings/listing.service';
import { ListingDetailVM } from '../../../core/models/listing.model';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-listings-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './listings-detail.html',
  styleUrls: ['./listings-detail.css'],
})
export class ListingsDetail implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private listingService = inject(ListingService);
  private sub: Subscription | null = null;

  listing?: ListingDetailVM;
  loading = true;
  error = '';
  currentImageIndex = 0;

  ngOnInit(): void {
    // Subscribe to route params so component reloads when navigated via routerLink
    this.sub = this.route.paramMap.subscribe((params) => {
      this.loading = true;
      this.error = '';
      this.currentImageIndex = 0;
      const idParam = params.get('id');
      if (idParam) {
        this.loadListing(+idParam);
      } else {
        this.error = 'Listing ID not provided';
        this.loading = false;
      }
    });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  private loadListing(id: number): void {
    this.listingService.getById(id).subscribe(
      (response) => {
        if (!response.isError && response.data) {
          this.listing = response.data;
        } else {
          this.error = response.message || 'Failed to load listing';
        }
        this.loading = false;
      },
      (err) => {
        this.error = 'Error loading listing details';
        this.loading = false;
      }
    );
  }

  nextImage(): void {
    if (this.listing?.images) {
      this.currentImageIndex = (this.currentImageIndex + 1) % this.listing.images.length;
    }
  }

  prevImage(): void {
    if (this.listing?.images) {
      this.currentImageIndex = 
        (this.currentImageIndex - 1 + this.listing.images.length) % this.listing.images.length;
    }
  }

  selectImage(index: number): void {
    this.currentImageIndex = index;
  }

  goBack(): void {
    this.router.navigate(['/listings']);
  }

  editListing(): void {
    if (this.listing) {
      this.router.navigate(['/listings', 'edit', this.listing.id]);
    }
  }
}
