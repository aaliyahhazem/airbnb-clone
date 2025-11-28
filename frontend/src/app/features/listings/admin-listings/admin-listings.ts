import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ListingService } from '../../../core/services/listings/listing.service';
import { ListingOverviewVM } from '../../../core/models/listing.model';

@Component({
  selector: 'app-admin-listings',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './admin-listings.html',
  styleUrls: ['./admin-listings.css']
})
export class AdminListingsComponent implements OnInit {
  private listingService = inject(ListingService);

  listings: ListingOverviewVM[] = [];
  loading = true;
  error = '';
  currentPage = 1;
  pageSize = 12;
  totalCount = 0;
  filterStatus: 'all' | 'approved' | 'pending' = 'all';

  ngOnInit(): void {
    this.loadListings();
  }

  loadListings(): void {
    this.loading = true;
    this.error = '';

    this.listingService.getAdminListings(this.currentPage, this.pageSize).subscribe(
      (response: any) => {
        if (!response.isError) {
          let filtered = response.data;
          if (this.filterStatus === 'approved') {
            filtered = filtered.filter((l: ListingOverviewVM) => l.isApproved);
          } else if (this.filterStatus === 'pending') {
            filtered = filtered.filter((l: ListingOverviewVM) => !l.isApproved);
          }
          this.listings = filtered;
          this.totalCount = response.totalCount;
        } else {
          this.error = response.message || 'Failed to load listings';
        }
        this.loading = false;
      },
      (err: any) => {
        this.error = 'Error loading listings';
        this.loading = false;
      }
    );
  }

  approveListing(id: number, event: Event): void {
    event.stopPropagation();
    const notes = prompt('Add notes (optional):');
    
    this.listingService.approveListing(id, notes || undefined).subscribe(
      (response) => {
        if (!response.isError) {
          this.listings = this.listings.map(l => 
            l.id === id ? { ...l, isApproved: true } : l
          );
        }
      }
    );
  }

  rejectListing(id: number, event: Event): void {
    event.stopPropagation();
    const notes = prompt('Rejection reason:');
    if (!notes) return;
    
    this.listingService.rejectListing(id, notes).subscribe(
      (response) => {
        if (!response.isError) {
          this.listings = this.listings.filter(l => l.id !== id);
        }
      }
    );
  }

  setFilter(status: 'all' | 'approved' | 'pending'): void {
    this.filterStatus = status;
    this.currentPage = 1;
    this.loadListings();
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
