// src/app/services/listing.service.ts
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { map, tap } from 'rxjs/operators';
import {
  Listing,
  ListingCreateVM,
  ListingDetailVM,
  ListingOverviewVM,
  ListingUpdateVM,
  ListingsResponse,
  ListingsPagedResponse
} from '../../models/listing.model';
import { UserPreferencesService } from '../user-preferences/user-preferences.service';

@Injectable({ providedIn: 'root' })
export class ListingService {
  private http = inject(HttpClient);
  private userPreferences = inject(UserPreferencesService);
  private apiUrl = 'http://localhost:5235/api/Listings';
  private backendOrigin = 'http://localhost:5235';

  private listingsSubject = new BehaviorSubject<ListingOverviewVM[]>([]);
  public listings$ = this.listingsSubject.asObservable();

  // Create a new listing
  create(vm: ListingCreateVM): Observable<ListingsResponse<number>> {
    const formData = this.buildFormData(vm);
    return this.http.post<any>(`${this.apiUrl}`, formData, {
      headers: { 'Accept-Language': 'en-US' }
    }).pipe(
      map(response => ({
        ...response,
        data: response.result ?? response.data,
        isError: response.isHaveErrorOrNo || response.isError || false,
        message: response.errorMessage ?? response.message
      }))
    );
  }

  // Get listing by ID with full details
  getById(id: number): Observable<ListingsResponse<ListingDetailVM>> {
    return this.http.get<any>(`${this.apiUrl}/${id}`).pipe(
      map(response => {
        // backend may return the payload under data or result depending on endpoint/version
        const payload = response?.data ?? response?.result ?? {};
        const res = {
          ...response,
          data: payload,
          isError: (response?.isHaveErrorOrNo ?? response?.isError) || false
        } as any;

        // Normalize image urls to absolute backend URLs
        if (res.data) {
          if (res.data.mainImageUrl) {
            res.data.mainImageUrl = this.normalizeImageUrl(res.data.mainImageUrl);
          }

          // Map host information
          if (res.data.hostId) {
            res.data.hostId = res.data.hostId;
          }
          if (res.data.hostName) {
            res.data.hostName = res.data.hostName;
          }

          if (Array.isArray(res.data.images)) {
            res.data.images = res.data.images.map((img: any) => ({
              ...img,
              imageUrl: this.normalizeImageUrl(img.imageUrl ?? img.ImageUrl ?? img.ImageUrl)
            }));
          }
        }

        return res;
      }),
      tap(response => {
        // Track user viewing this listing for personalization
        if (response.data && !response.isError) {
          const listing = response.data;
          const overviewVM: ListingOverviewVM = {
            id: listing.id,
            title: listing.title,
            pricePerNight: listing.pricePerNight,
            location: listing.location,
            mainImageUrl: listing.mainImageUrl,
            averageRating: listing.averageRating,
            reviewCount: listing.reviewCount,
            isApproved: listing.isApproved,
            description: listing.description,
            destination: listing.destination,
            type: listing.type,
            bedrooms: listing.bedrooms,
            bathrooms: listing.bathrooms,
            createdAt: listing.createdAt,
            priority: 0,
            viewCount: 0,
            favoriteCount: 0,
            bookingCount: 0,
            amenities: listing.amenities
          };
          this.userPreferences.trackListingInteraction(overviewVM, 1);
        }
      })
    );
  }

  // Get paginated listings (for public view)
  getPaged(page: number = 1, pageSize: number = 12, filter?: any): Observable<ListingsPagedResponse<ListingOverviewVM>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (filter) {
      if (filter.location) params = params.set('location', filter.location);
      if (filter.minPrice) params = params.set('minPrice', filter.minPrice);
      if (filter.maxPrice) params = params.set('maxPrice', filter.maxPrice);
      if (filter.minBedrooms) params = params.set('minBedrooms', filter.minBedrooms);
      if (filter.type) params = params.set('type', filter.type);
      if (filter.destination) params = params.set('destination', filter.destination);
    }

    return this.http.get<any>(`${this.apiUrl}`, { params }).pipe(
      map(response => {
      // Accept either result or data or the raw array
        const raw = response?.result ?? response?.data ?? response ?? [];
        const arr = Array.isArray(raw) ? raw : [];

        const normalized = arr.map((item: any) => ({
          id: Number(item.id ?? item.Id ?? item.listingId ?? item.ListingId ?? 0),
          title: item.title ?? item.Title ?? '',
          pricePerNight: item.pricePerNight ?? item.price ?? 0,
          location: item.location ?? item.Location ?? '',
          mainImageUrl: this.normalizeImageUrl(item.mainImageUrl ?? item.MainImageUrl ?? item.imageUrl ?? item.ImageUrl),
          averageRating: item.averageRating ?? item.rating ?? 0,
          reviewCount: item.reviewCount ?? 0,
          isApproved: !!(item.isApproved ?? item.IsApproved),
          description: item.description ?? item.Description ?? '',
          destination: item.destination ?? item.Destination ?? '',
          type: item.type ?? item.Type ?? '',
          bedrooms: item.bedrooms ?? item.Bedrooms ?? 0,
          bathrooms: item.bathrooms ?? item.Bathrooms ?? 0,
          createdAt: item.createdAt ?? item.CreatedAt ?? '',
          priority: item.priority ?? item.Priority ?? 0,
          viewCount: item.viewCount ?? item.ViewCount ?? 0,
          favoriteCount: item.favoriteCount ?? item.FavoriteCount ?? 0,
          bookingCount: item.bookingCount ?? item.BookingCount ?? 0,
          amenities: this.normalizeAmenities(item.amenities ?? item.Amenities ?? [])
        }));

        return {
          data: normalized as ListingOverviewVM[],
          totalCount: (response?.totalCount ?? normalized.length) || 0,
          message: response?.errorMessage ?? response?.message,
          isError: response?.isHaveErrorOrNo || response?.isError || false
        } as ListingsPagedResponse<ListingOverviewVM>;
      })
    );
  }

  // Get user's listings
  getUserListings(userId: string, page: number = 1, pageSize: number = 12): Observable<ListingsPagedResponse<ListingOverviewVM>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<ListingsPagedResponse<ListingOverviewVM>>(`${this.apiUrl}/my-listings`, { params });
  }

  // Get host's listings (including non-approved)
  getHostListings(page: number = 1, pageSize: number = 12): Observable<ListingsPagedResponse<ListingOverviewVM>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<any>(`${this.apiUrl}/my-listings`, { params }).pipe(
      map(response => {
        console.log('Raw backend response:', response);
        const raw = response?.result ?? response?.data ?? response ?? [];
        console.log('Raw array:', raw);
        const arr = Array.isArray(raw) ? raw : [];

        const normalized = arr.map((item: any) => ({
          id: Number(item.id ?? item.Id ?? item.listingId ?? item.ListingId ?? 0),
          title: item.title ?? item.Title ?? '',
          pricePerNight: item.pricePerNight ?? item.price ?? 0,
          location: item.location ?? item.Location ?? '',
          mainImageUrl: this.normalizeImageUrl(item.mainImageUrl ?? item.MainImageUrl ?? item.imageUrl ?? item.ImageUrl),
          averageRating: item.averageRating ?? item.rating ?? 0,
          reviewCount: item.reviewCount ?? 0,
          isApproved: item.isApproved ?? item.IsApproved ?? false,
          description: item.description ?? item.Description ?? '',
          destination: item.destination ?? item.Destination ?? '',
          type: item.type ?? item.Type ?? '',
          bedrooms: item.bedrooms ?? item.Bedrooms ?? 0,
          bathrooms: item.bathrooms ?? item.Bathrooms ?? 0
        }));

        return {
          data: normalized as ListingOverviewVM[],
          totalCount: (response?.totalCount ?? normalized.length) || 0,
          message: response?.errorMessage ?? response?.message,
          isError: response?.isHaveErrorOrNo || response?.isError || false
        } as ListingsPagedResponse<ListingOverviewVM>;
      })
    );
  }

  private sanitizeVmForJson(vm: ListingUpdateVM): any {
  const payload: any = {};
  if (vm.title !== undefined) payload.title = vm.title;
  if (vm.description !== undefined) payload.description = vm.description;
  if (vm.pricePerNight !== undefined) payload.pricePerNight = vm.pricePerNight;
  if (vm.location !== undefined) payload.location = vm.location;
  if (vm.latitude !== undefined) payload.latitude = vm.latitude;
  if (vm.longitude !== undefined) payload.longitude = vm.longitude;
  if (vm.maxGuests !== undefined) payload.maxGuests = vm.maxGuests;
  if (vm.destination !== undefined) payload.destination = vm.destination;
  if (vm.type !== undefined) payload.type = vm.type;
  if (vm.numberOfRooms !== undefined) payload.numberOfRooms = vm.numberOfRooms;
  if (vm.numberOfBathrooms !== undefined) payload.numberOfBathrooms = vm.numberOfBathrooms;
  if (vm.amenities !== undefined) payload.amenities = vm.amenities;
  if (vm.removeImageIds !== undefined) payload.removeImageIds = vm.removeImageIds;
  // do not include newImages in JSON payload
  return payload;
}
  // Update listing
  update(id: number, vm: ListingUpdateVM): Observable<ListingsResponse<ListingDetailVM>> {
    // Always send FormData for update to match backend [FromForm] binding
    const formData = this.buildFormData(vm);

    return this.http.put<any>(`${this.apiUrl}/${id}`, formData, {
      headers: { 'Accept-Language': 'en-US' }
    }).pipe(
      map(response => ({
        ...response,
        data: response.result ?? response.data ?? {},
        isError: response.isHaveErrorOrNo || response.isError || false
      }))
    );
}

  // Delete listing
  delete(id: number): Observable<ListingsResponse<boolean>> {
    return this.http.delete<any>(`${this.apiUrl}/${id}`).pipe(
      map(response => ({
        ...response,
        isError: response.isHaveErrorOrNo || response.isError || false
      }))
    );
  }

  // Admin: Get all listings with approval status
  getAdminListings(page: number = 1, pageSize: number = 12): Observable<ListingsPagedResponse<ListingOverviewVM>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    return this.http.get<any>(`${this.apiUrl}`, { params }).pipe(
      map(response => {
        const raw = response?.result ?? response?.data ?? response ?? [];
        const arr = Array.isArray(raw) ? raw : [];
        const normalized = arr.map((item: any) => ({
          id: Number(item.id ?? item.Id ?? 0),
          title: item.title ?? item.Title ?? '',
          pricePerNight: item.pricePerNight ?? item.price ?? 0,
          location: item.location ?? item.Location ?? '',
          mainImageUrl: this.normalizeImageUrl(item.mainImageUrl ?? item.MainImageUrl ?? item.imageUrl ?? item.ImageUrl),
          averageRating: item.averageRating ?? 0,
          reviewCount: item.reviewCount ?? 0,
          isApproved: item.isApproved ?? item.IsApproved ?? false,
          description: item.description ?? item.Description ?? '',
          destination: item.destination ?? item.Destination ?? '',
          type: item.type ?? item.Type ?? '',
          bedrooms: item.bedrooms ?? item.NumberOfRooms ?? 0,
          bathrooms: item.bathrooms ?? item.NumberOfBathrooms ?? 0
        }));
        return {
          data: normalized as ListingOverviewVM[],
          totalCount: (response?.totalCount ?? normalized.length) || 0,
          message: response?.errorMessage ?? response?.message,
          isError: response?.isHaveErrorOrNo || response?.isError || false
        } as ListingsPagedResponse<ListingOverviewVM>;
      })
    );
  }

  // Admin: Approve listing
  approveListing(id: number, notes?: string): Observable<ListingsResponse<boolean>> {
    return this.http.put<ListingsResponse<boolean>>(`${this.apiUrl}/admin/approve/listing/${id}`, { note: notes });
  }

  // Admin: Reject listing
  rejectListing(id: number, notes?: string): Observable<ListingsResponse<boolean>> {
    return this.http.put<ListingsResponse<boolean>>(`${this.apiUrl}/admin/reject/listing/${id}`, { note: notes });
  }

  // Helper: Build FormData for file uploads
  private buildFormData(vm: any): FormData {
    const formData = new FormData();

    // Required string fields
    if (vm.title) formData.append('title', String(vm.title).trim());
    if (vm.description) formData.append('description', String(vm.description).trim());
    if (vm.location) formData.append('location', String(vm.location).trim());
    if (vm.destination) formData.append('destination', String(vm.destination).trim());
    if (vm.type) formData.append('type', String(vm.type).trim());

    // Numeric fields - use locale-independent formatting
    if (vm.pricePerNight !== undefined && vm.pricePerNight !== null) {
      const price = Number(vm.pricePerNight);
      if (!isNaN(price) && price > 0) {
        // Use toLocaleString with en-US to ensure period as decimal separator
        formData.append('pricePerNight', price.toLocaleString('en-US'));
      }
    }

    if (vm.latitude !== undefined && vm.latitude !== null) {
      const lat = Number(vm.latitude);
      if (!isNaN(lat)) {
        formData.append('latitude', lat.toLocaleString('en-US'));
      }
    }

    if (vm.longitude !== undefined && vm.longitude !== null) {
      const lng = Number(vm.longitude);
      if (!isNaN(lng)) {
        formData.append('longitude', lng.toLocaleString('en-US'));
      }
    }

    if (vm.maxGuests !== undefined && vm.maxGuests !== null) {
      const guests = Number(vm.maxGuests);
      if (!isNaN(guests) && guests > 0) {
        formData.append('maxGuests', guests.toLocaleString('en-US'));
      }
    }

    // Room/bathroom counts
    if (vm.numberOfRooms !== undefined && vm.numberOfRooms !== null) {
      const rooms = Number(vm.numberOfRooms);
      if (!isNaN(rooms) && rooms > 0) {
        formData.append('numberOfRooms', rooms.toLocaleString('en-US'));
      }
    }

    if (vm.numberOfBathrooms !== undefined && vm.numberOfBathrooms !== null) {
      const baths = Number(vm.numberOfBathrooms);
      if (!isNaN(baths) && baths > 0) {
        formData.append('numberOfBathrooms', baths.toLocaleString('en-US'));
      }
    }

    // Amenities: use indexed format only (ASP.NET Core standard for list binding)
    if (vm.amenities && Array.isArray(vm.amenities) && vm.amenities.length > 0) {
      vm.amenities.forEach((amenity: string, index: number) => {
        if (amenity) {
          formData.append(`amenities[${index}]`, String(amenity).trim());
        }
      });
    }

    // Files: images (create) or newImages (update)
    if (vm.images && Array.isArray(vm.images) && vm.images.length > 0) {
      vm.images.forEach((file: File) => {
        if (file instanceof File) formData.append('images', file);
      });
    }

    if (vm.newImages && Array.isArray(vm.newImages) && vm.newImages.length > 0) {
      vm.newImages.forEach((file: File) => {
        if (file instanceof File) formData.append('newImages', file);
      });
    }

    // removeImageIds: append repeated numeric values
    if (vm.removeImageIds && Array.isArray(vm.removeImageIds) && vm.removeImageIds.length > 0) {
      vm.removeImageIds.forEach((id: number) => formData.append('removeImageIds', String(id)));
    }

    return formData;
  }

  private normalizeImageUrl(url?: string): string | undefined {
    if (!url) return undefined;
    const u = String(url);
    if (u.startsWith('http://') || u.startsWith('https://')) return u;
    if (u.startsWith('/')) return `${this.backendOrigin}${u}`;
    return `${this.backendOrigin}/${u}`;
  }

  private normalizeAmenities(amenities: any): string[] {
    if (!amenities) return [];

    // If it's already an array of strings, return it
    if (Array.isArray(amenities)) {
      // Check if array contains strings
      if (amenities.length > 0 && typeof amenities[0] === 'string') {
        return amenities
          .filter(a => a && typeof a === 'string')
          .map(a => a.trim())
          .filter(a => a.length > 0);
      }

      // Check if array contains objects with Word property (backend Amenity entity format)
      if (amenities.length > 0 && typeof amenities[0] === 'object') {
        return amenities
          .filter(a => a && (a.word || a.Word))
          .map(a => (a.word ?? a.Word).trim())
          .filter(a => a.length > 0);
      }
    }

    return [];
  }

  checkUserHasListings(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/has-listings`).pipe(
      map(response => ({
        hasListings: response?.hasListings ?? false,
        message: response?.message ?? '',
        isError: response?.isError ?? false
      }))
    );
  }
}
