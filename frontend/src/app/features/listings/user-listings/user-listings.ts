import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { ListingService } from '../../../core/services/listings/listing.service';
import { ListingOverviewVM } from '../../../core/models/listing.model';

@Component({
  selector: 'app-user-listings',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './user-listings.html',
  styleUrls: ['./user-listings.css']
})
export class UserListingsComponent implements OnInit {
  private listingService = inject(ListingService);
  private router = inject(Router);

  listings: ListingOverviewVM[] = [];
  loading = true;
  error = '';
  currentPage = 1;
  pageSize = 12;
  totalCount = 0;

  ngOnInit(): void {
    this.loadListings();
  }

  loadListings(): void {
    this.loading = true;
    this.error = '';

    // Get user ID from local storage or auth service
    const userId = localStorage.getItem('userId');
    if (!userId) {
      this.error = 'Please log in to view your listings';
      this.loading = false;
      return;
    }

    this.listingService.getUserListings(userId, this.currentPage, this.pageSize).subscribe(
      (response) => {
        if (!response.isError) {
          this.listings = response.data;
          this.totalCount = response.totalCount;
        } else {
          this.error = response.message || 'Failed to load listings';
        }
        this.loading = false;
      },
      (err) => {
        this.error = 'Error loading listings';
        this.loading = false;
      }
    );
  }

  createListing(): void {
    this.router.navigate(['/listings/create']);
  }

  editListing(id: number): void {
    this.router.navigate(['/listings', id, 'edit']);
  }

  viewListing(id: number): void {
    this.router.navigate(['/listings', id]);
  }

  deleteListing(id: number, event: Event): void {
    event.stopPropagation();
    if (!confirm('Are you sure you want to delete this listing?')) return;

    this.listingService.delete(id).subscribe(
      (response) => {
        if (!response.isError) {
          this.listings = this.listings.filter(l => l.id !== id);
        }
      }
    );
  }

  get totalPages(): number {
    return Math.ceil(this.totalCount / this.pageSize);
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.loadListings();
    }
  }
}
