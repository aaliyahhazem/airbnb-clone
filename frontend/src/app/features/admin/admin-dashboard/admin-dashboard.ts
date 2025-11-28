import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import {
  AdminService,
  SystemStats,
  UserAdmin,
  ListingAdmin,
  Booking,
  RevenuePoint,
  PromotionSummary,
} from '../../../core/services/admin.service';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './admin-dashboard.html',
  styleUrls: ['./admin-dashboard.css'],
})
export class AdminDashboard implements OnInit {
  private adminService = inject(AdminService);
  private cdr = inject(ChangeDetectorRef);

  // Main UI state
  activeTab: string = 'dashboard';
  isLoading = false;

  // Dashboard Stats
  stats: SystemStats | null = null;

  // Users/Guests Management
  users: UserAdmin[] = [];
  userSearchQuery: string = '';
  userRoleFilter: string = '';
    userActiveFilter: boolean | undefined = undefined;
  usersPage = 1;
  usersPageSize = 20;

  // Listings Management
  listings: ListingAdmin[] = [];
  listingStatusFilter: string = '';
  listingsPage = 1;
  listingsPageSize = 20;
  pendingListings: ListingAdmin[] = [];

  // Bookings Management
  bookings: Booking[] = [];
  bookingsPage = 1;
  bookingsPageSize = 50;

  // Revenue & Analytics
  revenueData: RevenuePoint[] = [];
  revenueMonths = 12;

  // Promotions Management
  activePromotions: PromotionSummary[] = [];
  promotionHistory: PromotionSummary[] = [];
  expiringPromotions: PromotionSummary[] = [];
  // Error display
  errorMessage: string | null = null;

  ngOnInit(): void {
    this.loadStats();
  }

  loadStats(): void {
    this.isLoading = true;
    this.adminService.getStats().subscribe({
      next: (stats) => {
        this.stats = stats;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: (error: any) => this.handleError('loading stats', error),
    });
  }

  onTabChange(tab: string): void {
    this.activeTab = tab;

    switch (tab) {
      case 'guests':
        this.loadUsers();
        break;
      case 'listings':
        this.loadListings();
        break;
      case 'listings-pending':
        this.loadPendingListings();
        break;
      case 'bookings':
        this.loadBookings();
        break;
      case 'revenue':
        this.loadRevenue();
        break;
      case 'promotions':
        this.loadPromotions();
        break;
      case 'dashboard':
        this.loadStats();
        break;
    }
  }

  // ===== USERS / GUESTS =====
  loadUsers(): void {
    this.isLoading = true;
    this.adminService.getUsersFiltered(
      this.userSearchQuery || undefined,
      this.userRoleFilter || undefined,
      this.userActiveFilter,
      this.usersPage,
      this.usersPageSize
    ).subscribe({
      next: (users) => {
        this.users = users;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: (error: any) => this.handleError('loading users', error),
    });
  }

  searchUsers(): void {
    this.usersPage = 1;
    this.loadUsers();
  }

  deactivateUser(userId: string): void {
    if (!confirm('Deactivate this user?')) return;
    this.adminService.deactivateUser(userId).subscribe({
      next: () => this.loadUsers(),
      error: (error: any) => this.handleError('deactivating user', error),
    });
  }

  // ===== LISTINGS =====
  loadListings(): void {
    this.isLoading = true;
    this.adminService.getListingsFiltered(
      this.listingStatusFilter || undefined,
      this.listingsPage,
      this.listingsPageSize
    ).subscribe({
      next: (listings) => {
        this.listings = listings;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: (error: any) => this.handleError('loading listings', error),
    });
  }

  filterListings(): void {
    this.listingsPage = 1;
    this.loadListings();
  }

  loadPendingListings(): void {
    this.isLoading = true;
    this.adminService.getListingsPendingApproval().subscribe({
      next: (listings) => {
        this.pendingListings = listings;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: (error: any) => this.handleError('loading pending listings', error),
    });
  }

  approveListing(listingId: number): void {
    if (!confirm('Approve this listing?')) return;
    this.adminService.approveListing(listingId).subscribe({
      next: (ok) => {
        if (ok) {
          if (this.activeTab === 'listings-pending') this.loadPendingListings();
          else this.loadListings();
        }
      },
      error: (error: any) => this.handleError('approving listing', error),
    });
  }

  rejectListing(listingId: number): void {
    const note = prompt('Enter rejection note (optional):');
    if (!confirm('Reject this listing?')) return;
    this.adminService.rejectListing(listingId, note || undefined).subscribe({
      next: (ok) => {
        if (ok) {
          if (this.activeTab === 'listings-pending') this.loadPendingListings();
          else this.loadListings();
        }
      },
      error: (error: any) => this.handleError('rejecting listing', error),
    });
  }

  promoteListing(listingId: number): void {
    const daysStr = prompt('Promote for how many days? (enter number)');
    const days = Math.max(1, parseInt(daysStr || '7', 10) || 7);
    const end = new Date();
    end.setDate(end.getDate() + days);
    const iso = end.toISOString();
    if (!confirm(`Promote listing until ${end.toLocaleString()}?`)) return;
    this.adminService.promoteListing(listingId, iso).subscribe({
      next: (ok) => {
        if (ok) this.loadListings();
      },
      error: (error: any) => this.handleError('promoting listing', error),
    });
  }

  unpromoteListing(listingId: number): void {
    if (!confirm('Cancel promotion for this listing?')) return;
    this.adminService.unpromoteListing(listingId).subscribe({
      next: (ok) => {
        if (ok) this.loadListings();
      },
      error: (error: any) => this.handleError('unpromoting listing', error),
    });
  }

  extendPromotion(listingId: number): void {
    const daysStr = prompt('Extend promotion by how many days? (enter number)');
    const days = Math.max(1, parseInt(daysStr || '7', 10) || 7);
    const end = new Date();
    end.setDate(end.getDate() + days);
    const iso = end.toISOString();
    if (!confirm(`Extend promotion until ${end.toLocaleString()}?`)) return;
    this.adminService.extendPromotion(listingId, iso).subscribe({
      next: (ok) => {
        if (ok) this.loadListings();
      },
      error: (error: any) => this.handleError('extending promotion', error),
    });
  }

  // ===== BOOKINGS =====
  loadBookings(): void {
    this.isLoading = true;
    this.adminService.getBookings(this.bookingsPage, this.bookingsPageSize).subscribe({
      next: (bookings) => {
        this.bookings = bookings;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: (error: any) => this.handleError('loading bookings', error),
    });
  }

  // ===== REVENUE =====
  loadRevenue(): void {
    this.isLoading = true;
    this.adminService.getRevenueTrend(this.revenueMonths).subscribe({
      next: (revenue) => {
        this.revenueData = revenue;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: (error: any) => this.handleError('loading revenue', error),
    });
  }

  getTotalRevenue(): number {
    return this.revenueData.reduce((sum, point) => sum + point.total, 0);
  }

  // ===== PROMOTIONS =====
  loadPromotions(): void {
    this.isLoading = true;
    let completed = 0;

    const checkComplete = () => {
      if (++completed === 3) {
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    };

    this.adminService.getActivePromotions().subscribe({
      next: (promos) => {
        this.activePromotions = promos;
        checkComplete();
      },
      error: (error: any) => { this.handleError('loading active promotions', error); checkComplete(); },
    });

    this.adminService.getPromotionsHistory().subscribe({
      next: (history) => {
        this.promotionHistory = history;
        checkComplete();
      },
      error: (error: any) => { this.handleError('loading promotion history', error); checkComplete(); },
    });

    this.adminService.getExpiringPromotions(7).subscribe({
      next: (expiring) => {
        this.expiringPromotions = expiring;
        checkComplete();
      },
      error: (error: any) => { this.handleError('loading expiring promotions', error); checkComplete(); },
    });
  }

  // Centralized error handler
  private handleError(context: string, error: any): void {
    console.error(`Error ${context}:`, error);
    try {
      const msg = error?.message || error?.error?.errorMessage || error?.statusText || JSON.stringify(error);
      this.errorMessage = `${context}: ${msg}`;
    } catch (e) {
      this.errorMessage = `${context}: unknown error`;
    }
    this.isLoading = false;
    this.cdr.markForCheck();
  }

  // ===== PAGINATION =====
  nextPage(section: string): void {
    if (section === 'users') this.usersPage++;
    else if (section === 'listings') this.listingsPage++;
    else if (section === 'bookings') this.bookingsPage++;

    this.loadSectionData(section);
  }

  previousPage(section: string): void {
    if (section === 'users' && this.usersPage > 1) this.usersPage--;
    else if (section === 'listings' && this.listingsPage > 1) this.listingsPage--;
    else if (section === 'bookings' && this.bookingsPage > 1) this.bookingsPage--;

    this.loadSectionData(section);
  }

  private loadSectionData(section: string): void {
    if (section === 'users') this.loadUsers();
    else if (section === 'listings') this.loadListings();
    else if (section === 'bookings') this.loadBookings();
  }

  // ===== HELPER METHODS =====
  getTabLabel(tab: string): string {
    const labels: Record<string, string> = {
      dashboard: 'Dashboard',
      guests: 'Guests',
      listings: 'All Listings',
      'listings-pending': 'Pending',
      bookings: 'Bookings',
      revenue: 'Revenue',
      promotions: 'Promotions',
    };
    return labels[tab] || tab;
  }

  getDaysRemaining(endDate?: Date | string | null): number {
    if (!endDate) return 0;
    const end = typeof endDate === 'string' ? new Date(endDate) : endDate;
    const today = new Date();
    const diff = end.getTime() - today.getTime();
    return Math.max(0, Math.ceil(diff / (1000 * 60 * 60 * 24)));
  }

  getDaysRemainingClass(endDate?: Date | string | null): string {
    const days = this.getDaysRemaining(endDate);
    if (days <= 3) return 'expiry-critical';
    if (days <= 7) return 'expiry-warning';
    return 'expiry-normal';
  }

  getDaysBetween(startDate?: Date | string | null, endDate?: Date | string | null): number {
    if (!startDate || !endDate) return 0;
    const start = typeof startDate === 'string' ? new Date(startDate) : startDate;
    const end = typeof endDate === 'string' ? new Date(endDate) : endDate;
    const diff = end.getTime() - start.getTime();
    return Math.ceil(diff / (1000 * 60 * 60 * 24));
  }
}
