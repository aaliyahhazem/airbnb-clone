// core/services/admin.service.ts
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';

export interface UserSummary {
  id: string;
  email: string;
  fullName: string;
  role: string;
  isActive: boolean;
}

export interface UserAdmin {
  id: string;
  email: string;
  fullName: string;
  role: string;
  isActive: boolean;
  totalBookings: number;
  totalListings: number;
  bookingsCount?: number;
  createdAt?: string | Date;
}

export interface Listing {
  id: number;
  title: string;
  location: string;
  pricePerNight: number;
  isApproved: boolean;
  isReviewed: boolean;
  isPromoted: boolean;
  promotionEndDate?: string | null;
  hostId: string;
  hostName: string;
  reviewsCount: number;
}

export interface ListingAdmin {
  id: number;
  title: string;
  location: string;
  pricePerNight: number;
  isApproved: boolean;
  isReviewed: boolean;
  isPromoted: boolean;
  promotionEndDate?: string | null;
  hostId: string;
  hostName: string;
  reviewsCount: number;
  status?: string;
  description?: string;
  createdAt?: string | Date;
}

export interface Booking {
  id: number;
  listingId: number;
  listingTitle: string;
  guestId: string;
  guestName: string;
  hostId?: string;
  hostName?: string;
  checkIn: string | Date;
  checkOut: string | Date;
  totalPrice: number;
  bookingStatus?: string;
  paymentStatus?: string;
  status?: string;
  checkInDate?: string | Date;
  checkOutDate?: string | Date;
  paymentId?: number;
  paymentAmount?: number;
  paidAt?: string | Date;
}

export interface SystemStats {
  totalUsers: number;
  totalListings: number;
  totalBookings: number;
  pendingListingsCount?: number;
  activePromotionsCount?: number;
  totalHosts?: number;
}

export interface RevenuePoint {
  year: number;
  month: number;
  total: number;
  monthDate?: string | Date;
  transactionCount?: number;
}

export interface PromotionSummary {
  listingId: number;
  title: string;
  listingTitle?: string;
  hostId: string;
  hostName: string;
  promotionEndDate?: string | null;
  startDate?: string | Date;
  endDate?: string | Date;
  isActive: boolean;
}

export interface ApiResponse<T> {
  result: T;
  errorMessage?: string | null;
  IsHaveErrorOrNo?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private http = inject(HttpClient);
  private baseUrl = 'http://localhost:5235/api';

  // Users Management
  getUsers(): Observable<UserSummary[]> {
    return this.http.get<ApiResponse<UserSummary[]>>(`${this.baseUrl}/admin/users`)
      .pipe(
        map(response => response.result || []),
        catchError(error => {
          console.error('Error fetching users:', error);
          return of([]);
        })
      );
  }

  getUserById(id: string): Observable<UserSummary> {
    return this.http.get<ApiResponse<UserSummary>>(`${this.baseUrl}/admin/users/${id}`)
      .pipe(
        map(response => response.result),
        catchError(error => {
          console.error('Error fetching user:', error);
          return of({} as UserSummary);
        })
      );
  }

  getUsersFiltered(search?: string, role?: string, isActive?: boolean, page: number = 1, pageSize: number = 50): Observable<UserAdmin[]> {
    let params = new HttpParams();
    if (search) params = params.set('search', search);
    if (role) params = params.set('role', role);
    if (isActive !== undefined) params = params.set('isActive', isActive.toString());
    params = params.set('page', page.toString()).set('pageSize', pageSize.toString());
    
    return this.http.get<ApiResponse<any[]>>(`${this.baseUrl}/admin/users`, { params })
      .pipe(
        map(response => (response.result || []).map(item => ({
          id: item.id,
          email: item.email,
          fullName: item.fullName,
          role: item.role,
          isActive: item.isActive,
          totalBookings: item.totalBookings ?? item.totalBookingsCount ?? 0,
          totalListings: item.totalListings ?? 0,
          bookingsCount: item.totalBookings ?? 0,
          createdAt: item.createdAt ?? item.createdAtUtc ?? null
        }) as UserAdmin)),
        catchError(error => {
          console.error('Error fetching filtered users:', error);
          return of([]);
        })
      );
  }

  deactivateUser(id: string): Observable<boolean> {
    return this.http.put<ApiResponse<boolean>>(`${this.baseUrl}/admin/users/${id}/deactivate`, {})
      .pipe(
        map(r => (r.result as unknown as boolean) || false),
        catchError(error => {
          console.error('Error deactivating user:', error);
          return of(false);
        })
      );
  }

  // Stats
  getStats(): Observable<SystemStats> {
    return this.http.get<ApiResponse<any>>(`${this.baseUrl}/admin/stats`)
      .pipe(
        map(response => {
          const r = response.result || {};
          return {
            totalUsers: r.totalUsers ?? r.totalUsersCount ?? 0,
            totalListings: r.totalListings ?? 0,
            totalBookings: r.totalBookings ?? 0,
            pendingListingsCount: r.pendingListingsCount ?? 0,
            activePromotionsCount: r.activePromotionsCount ?? 0,
            totalHosts: r.totalHosts ?? 0
          } as SystemStats;
        }),
        catchError(error => {
          console.error('Error fetching stats:', error);
          return of({ totalUsers: 0, totalListings: 0, totalBookings: 0 } as SystemStats);
        })
      );
  }

  // Listings Management
  getAllListings(page: number = 1, pageSize: number = 10): Observable<Listing[]> {
    const params = { page: page.toString(), pageSize: pageSize.toString() };
    return this.http.get<ApiResponse<Listing[]>>(`${this.baseUrl}/Listings/admin/all-listings`, { params })
      .pipe(
        map(response => response.result || []),
        catchError(error => {
          console.error('Error fetching all listings:', error);
          return of([]);
        })
      );
  }

  getListingsFiltered(status?: string, page: number = 1, pageSize: number = 50): Observable<ListingAdmin[]> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    params = params.set('page', page.toString()).set('pageSize', pageSize.toString());
    
    return this.http.get<ApiResponse<any[]>>(`${this.baseUrl}/admin/listings`, { params })
      .pipe(
        map(response => (response.result || []).map(item => ({
          id: item.id,
          title: item.title,
          location: item.location,
          pricePerNight: item.pricePerNight ?? item.price ?? 0,
          isApproved: item.isApproved ?? item.isApproved,
          isReviewed: item.isReviewed ?? false,
          isPromoted: item.isPromoted ?? false,
          promotionEndDate: item.promotionEndDate ?? item.promotionEndDate ?? null,
          hostId: item.hostId ?? item.hostId,
          hostName: item.hostName ?? item.hostName,
          reviewsCount: item.reviewsCount ?? item.reviewsCount ?? 0,
          status: item.status ?? (item.isPromoted ? 'promoted' : (item.isApproved ? 'approved' : 'pending')),
          description: item.description ?? item.description ?? '',
          createdAt: item.createdAt ?? null
        }) as ListingAdmin)),
        catchError(error => {
          console.error('Error fetching filtered listings:', error);
          return of([]);
        })
      );
  }

  getListingsPendingApproval(): Observable<ListingAdmin[]> {
    return this.http.get<ApiResponse<any[]>>(`${this.baseUrl}/admin/listings/pending`)
      .pipe(
        map(response => (response.result || []).map(item => ({
          id: item.id,
          title: item.title,
          location: item.location,
          pricePerNight: item.pricePerNight ?? 0,
          isApproved: item.isApproved ?? false,
          isReviewed: item.isReviewed ?? false,
          isPromoted: item.isPromoted ?? false,
          promotionEndDate: item.promotionEndDate ?? null,
          hostId: item.hostId ?? null,
          hostName: item.hostName ?? item.hostName,
          reviewsCount: item.reviewsCount ?? 0,
          status: item.status ?? 'pending',
          description: item.description ?? '',
          createdAt: item.createdAt ?? null
        }) as ListingAdmin)),
        catchError(error => {
          console.error('Error fetching pending listings:', error);
          return of([]);
        })
      );
  }

  approveListing(listingId: number): Observable<boolean> {
    return this.http.put<ApiResponse<boolean>>(`${this.baseUrl}/Listings/admin/approve/listing/${listingId}`, {})
      .pipe(
        map(r => (r.result as unknown as boolean) || false),
        catchError(error => {
          console.error('Error approving listing:', error);
          return of(false);
        })
      );
  }

  rejectListing(listingId: number, note?: string): Observable<boolean> {
    const body = note ? { note } : {};
    return this.http.put<ApiResponse<boolean>>(`${this.baseUrl}/Listings/admin/reject/listing/${listingId}`, body)
      .pipe(
        map(r => (r.result as unknown as boolean) || false),
        catchError(error => {
          console.error('Error rejecting listing:', error);
          return of(false);
        })
      );
  }

  promoteListing(listingId: number, promotionEndDate: string): Observable<boolean> {
    return this.http.post<ApiResponse<boolean>>(`${this.baseUrl}/Listings/admin/promote/${listingId}`, { PromotionEndDate: promotionEndDate })
      .pipe(
        map(r => (r.result as unknown as boolean) || false),
        catchError(error => {
          console.error('Error promoting listing:', error);
          return of(false);
        })
      );
  }

  unpromoteListing(listingId: number): Observable<boolean> {
    return this.http.post<ApiResponse<boolean>>(`${this.baseUrl}/Listings/admin/unpromote/${listingId}`, {})
      .pipe(
        map(r => (r.result as unknown as boolean) || false),
        catchError(error => {
          console.error('Error unpromoting listing:', error);
          return of(false);
        })
      );
  }

  extendPromotion(listingId: number, promotionEndDate: string): Observable<boolean> {
    return this.http.post<ApiResponse<boolean>>(`${this.baseUrl}/Listings/admin/extend-promotion/${listingId}`, { PromotionEndDate: promotionEndDate })
      .pipe(
        map(r => (r.result as unknown as boolean) || false),
        catchError(error => {
          console.error('Error extending promotion:', error);
          return of(false);
        })
      );
  }

  // Bookings
  getBookings(page: number = 1, pageSize: number = 100): Observable<Booking[]> {
    const params = { page: page.toString(), pageSize: pageSize.toString() };
    return this.http.get<ApiResponse<any[]>>(`${this.baseUrl}/admin/bookings`, { params })
      .pipe(
        map(response => (response.result || []).map(item => ({
          id: item.id,
          listingId: item.listingId ?? item.listingId,
          listingTitle: item.listingTitle ?? item.listingTitle ?? '',
          guestId: item.guestId ?? item.guestId,
          guestName: item.guestName ?? item.guestName ?? '',
          hostId: item.hostId ?? item.hostId,
          hostName: item.hostName ?? item.hostName ?? '',
          checkIn: item.checkInDate ?? item.checkIn ?? null,
          checkOut: item.checkOutDate ?? item.checkOut ?? null,
          totalPrice: item.totalPrice ?? item.totalPrice ?? 0,
          bookingStatus: item.bookingStatus ?? item.status ?? null,
          paymentStatus: item.paymentStatus ?? null,
          paymentId: item.paymentId ?? null,
          paymentAmount: item.paymentAmount ?? null,
          paidAt: item.paidAt ?? null
        }) as Booking)),
        catchError(error => {
          console.error('Error fetching bookings:', error);
          return of([]);
        })
      );
  }

  // Revenue & Analytics
  getRevenueTrend(months: number = 12): Observable<RevenuePoint[]> {
    const params = { months: months.toString() };
    return this.http.get<ApiResponse<any[]>>(`${this.baseUrl}/admin/revenue/trend`, { params })
      .pipe(
        map(response => (response.result || []).map(item => ({
          year: item.year ?? item.year,
          month: item.month ?? item.month,
          total: item.total ?? 0,
          monthDate: (() => {
            try { return new Date((item.year ?? 1970), ((item.month ?? 1) - 1), 1); } catch { return null; }
          })(),
          transactionCount: item.transactionCount ?? 0
        }) as RevenuePoint)),
        catchError(error => {
          console.error('Error fetching revenue trend:', error);
          return of([]);
        })
      );
  }

  // Promotions
  getActivePromotions(): Observable<PromotionSummary[]> {
    return this.http.get<ApiResponse<any[]>>(`${this.baseUrl}/admin/promotions/active`)
      .pipe(
        map(response => (response.result || []).map(item => ({
          listingId: item.listingId,
          title: item.title ?? item.title ?? item.listingTitle ?? '',
          listingTitle: item.title ?? item.listingTitle ?? '',
          hostId: item.hostId ?? item.hostId,
          hostName: item.hostName ?? item.hostName ?? '',
          promotionEndDate: item.promotionEndDate ?? item.promotionEndDate ?? null,
          startDate: item.startDate ?? null,
          endDate: item.promotionEndDate ?? null,
          isActive: item.isActive ?? true
        }) as PromotionSummary)),
        catchError(error => {
          console.error('Error fetching active promotions:', error);
          return of([]);
        })
      );
  }

  getPromotionsHistory(from?: Date, to?: Date): Observable<PromotionSummary[]> {
    let params = new HttpParams();
    if (from) params = params.set('from', from.toISOString());
    if (to) params = params.set('to', to.toISOString());
    
    return this.http.get<ApiResponse<any[]>>(`${this.baseUrl}/admin/promotions/history`, { params })
      .pipe(
        map(response => (response.result || []).map(item => ({
          listingId: item.listingId,
          title: item.title ?? item.listingTitle ?? '',
          listingTitle: item.title ?? item.listingTitle ?? '',
          hostId: item.hostId ?? item.hostId,
          hostName: item.hostName ?? item.hostName ?? '',
          promotionEndDate: item.promotionEndDate ?? null,
          startDate: item.startDate ?? null,
          endDate: item.promotionEndDate ?? null,
          isActive: item.isActive ?? false
        }) as PromotionSummary)),
        catchError(error => {
          console.error('Error fetching promotion history:', error);
          return of([]);
        })
      );
  }

  getExpiringPromotions(days: number = 7): Observable<PromotionSummary[]> {
    const params = { days: days.toString() };
    return this.http.get<ApiResponse<any[]>>(`${this.baseUrl}/admin/promotions/expiring`, { params })
      .pipe(
        map(response => (response.result || []).map(item => ({
          listingId: item.listingId,
          title: item.title ?? item.listingTitle ?? '',
          listingTitle: item.title ?? item.listingTitle ?? '',
          hostId: item.hostId ?? item.hostId,
          hostName: item.hostName ?? item.hostName ?? '',
          promotionEndDate: item.promotionEndDate ?? null,
          startDate: item.startDate ?? null,
          endDate: item.promotionEndDate ?? null,
          isActive: item.isActive ?? false
        }) as PromotionSummary)),
        catchError(error => {
          console.error('Error fetching expiring promotions:', error);
          return of([]);
        })
      );
  }
}
