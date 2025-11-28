// src/app/services/listing.service.ts
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import {
  Listing,
  ListingCreateVM,
  ListingDetailVM,
  ListingOverviewVM,
  ListingUpdateVM,
  ListingsResponse,
  ListingsPagedResponse
} from '../../models/listing.model';

@Injectable({ providedIn: 'root' })
export class ListingService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5235/api/Listings';
  private backendOrigin = 'http://localhost:5235';

  private listingsSubject = new BehaviorSubject<ListingOverviewVM[]>([]);
  public listings$ = this.listingsSubject.asObservable();

  // Create a new listing
  create(vm: ListingCreateVM): Observable<ListingsResponse<number>> {
    const formData = this.buildFormData(vm);
    // debug: inspect form data
    // for (const pair of formData.entries()) console.log('[Create] formData', pair[0], pair[1]);
    return this.http.post<any>(`${this.apiUrl}`, formData).pipe(
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
        const payload = response?.data ?? response?.result ?? {};
        const res = {
          ...response,
          data: payload,
          isError: (response?.isHaveErrorOrNo ?? response?.isError) || false
        } as any;

        if (res.data) {
          if (res.data.mainImageUrl) {
            res.data.mainImageUrl = this.normalizeImageUrl(res.data.mainImageUrl);
          }

          if (Array.isArray(res.data.images)) {
            res.data.images = res.data.images.map((img: any) => ({
              ...img,
              imageUrl: this.normalizeImageUrl(img.imageUrl ?? img.ImageUrl ?? img.ImageUrl)
            }));
          }
        }

        return res;
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
    // Debug: print FormData entries so you can inspect what is actually sent
    try {
      console.log('--- FormData entries for update ---');
      for (const pair of (formData as any).entries()) {
        console.log(pair[0], pair[1]);
      }
    } catch (e) {
      console.log('Could not iterate FormData entries', e);
    }

    return this.http.put<any>(`${this.apiUrl}/${id}`, formData).pipe(
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

    if (vm.title !== undefined) formData.append('title', String(vm.title));
    if (vm.description !== undefined) formData.append('description', String(vm.description));
    if (vm.pricePerNight !== undefined) formData.append('pricePerNight', String(vm.pricePerNight));
    if (vm.location !== undefined) formData.append('location', String(vm.location));
    if (vm.latitude !== undefined) formData.append('latitude', String(vm.latitude));
    if (vm.longitude !== undefined) formData.append('longitude', String(vm.longitude));
    if (vm.maxGuests !== undefined) formData.append('maxGuests', String(vm.maxGuests));

    // Destination & Type (required by backend)
    if (vm.destination !== undefined) formData.append('destination', String(vm.destination));
    if (vm.type !== undefined) formData.append('type', String(vm.type));

    if (vm.numberOfRooms !== undefined) formData.append('numberOfRooms', String(vm.numberOfRooms));
    if (vm.numberOfBathrooms !== undefined) formData.append('numberOfBathrooms', String(vm.numberOfBathrooms));

    // Amenities: append both repeated and indexed forms to maximize binder compatibility
    if (vm.amenities && vm.amenities.length > 0) {
      vm.amenities.forEach((amenity: string, index: number) => {
        formData.append(`amenities[${index}]`, amenity);
        formData.append('amenities', amenity);
      });
    }

    // Files: images (create) or newImages (update)
    if (vm.images && vm.images.length > 0) {
      vm.images.forEach((file: File) => formData.append('images', file));
    }
    if (vm.newImages && vm.newImages.length > 0) {
      vm.newImages.forEach((file: File) => formData.append('newImages', file));
    }

    // removeImageIds: append repeated numeric values
    if (vm.removeImageIds && vm.removeImageIds.length > 0) {
      vm.removeImageIds.forEach((id: number) => formData.append('removeImageIds', String(id)));
      // also send JSON if backend expects a single JSON string (safe extra)
      formData.append('removeImageIdsJson', JSON.stringify(vm.removeImageIds));
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
}