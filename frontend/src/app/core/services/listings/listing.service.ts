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
    return this.http.post<any>(`${this.apiUrl}`, formData).pipe(
      map(response => ({
        ...response,
        data: response.result ?? response.data,
        isError: response.isHaveErrorOrNo || false,
        message: response.errorMessage
      }))
    );
  }

  // Get listing by ID with full details
  getById(id: number): Observable<ListingsResponse<ListingDetailVM>> {
    return this.http.get<any>(`${this.apiUrl}/${id}`).pipe(
      map(response => {
        // backend may return the payload under `data` or `result` depending on endpoint/version
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

          if (Array.isArray(res.data.images)) {
            res.data.images = res.data.images.map((img: any) => ({
              ...img,
              imageUrl: this.normalizeImageUrl(img.imageUrl ?? img.ImageUrl ?? img.imageUrl)
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
  }

  return this.http.get<any>(`${this.apiUrl}`, { params }).pipe(
    map(response => {
      // Accept either `result` or `data` or the raw array
      const raw = response?.result ?? response?.data ?? response ?? [];
      const arr = Array.isArray(raw) ? raw : [];

      const normalized = arr.map((item: any) => ({
        id: Number(item.id ?? item.Id ?? item.listingId ?? item.ListingId ?? 0),
        title: item.title ?? item.Title ?? '',
        pricePerNight: item.pricePerNight ?? item.price ?? 0,
        location: item.location ?? item.Location ?? '',
        mainImageUrl: this.normalizeImageUrl(item.mainImageUrl ?? item.MainImageUrl ?? item.imageUrl ?? item.ImageUrl),
        averageRating: item.averageRating ?? item.averageRating ?? item.rating ?? 0,
        reviewCount: item.reviewCount ?? 0,
        isApproved: !!item.isApproved,
        description: item.description ?? item.Description ?? ''
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

        const normalized = arr.map((item: any) => {
          console.log('Item before normalization:', item);
          const normalized = {
            id: Number(item.id ?? item.Id ?? item.listingId ?? item.ListingId ?? 0),
            title: item.title ?? item.Title ?? '',
            pricePerNight: item.pricePerNight ?? item.price ?? 0,
            location: item.location ?? item.Location ?? '',
            mainImageUrl: this.normalizeImageUrl(item.mainImageUrl ?? item.MainImageUrl ?? item.imageUrl ?? item.ImageUrl),
            averageRating: item.averageRating ?? item.averageRating ?? item.rating ?? 0,
            reviewCount: item.reviewCount ?? 0,
            isApproved: item.isApproved ?? item.IsApproved ?? false,
            description: item.description ?? item.Description ?? ''
          };
          console.log('Item after normalization:', normalized);
          return normalized;
        });

        return {
          data: normalized as ListingOverviewVM[],
          totalCount: (response?.totalCount ?? normalized.length) || 0,
          message: response?.errorMessage ?? response?.message,
          isError: response?.isHaveErrorOrNo || response?.isError || false
        } as ListingsPagedResponse<ListingOverviewVM>;
      })
    );
  }

  // Update listing
  update(id: number, vm: ListingUpdateVM): Observable<ListingsResponse<ListingDetailVM>> {
    const formData = this.buildFormData(vm);
    return this.http.put<any>(`${this.apiUrl}/${id}`, formData).pipe(
      map(response => ({
        ...response,
        data: response.result || {},
        isError: response.isHaveErrorOrNo || false
      }))
    );
  }

  // Delete listing
  delete(id: number): Observable<ListingsResponse<boolean>> {
    return this.http.delete<any>(`${this.apiUrl}/${id}`).pipe(
      map(response => ({
        ...response,
        isError: response.isHaveErrorOrNo || false
      }))
    );
  }

  // Admin: Get all listings with approval status
  getAdminListings(page: number = 1, pageSize: number = 12): Observable<ListingsPagedResponse<ListingOverviewVM>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    // This endpoint may not exist yet - might need to adjust based on backend
    return this.http.get<ListingsPagedResponse<ListingOverviewVM>>(`${this.apiUrl}`, { params });
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
    
    formData.append('title', vm.title);
    formData.append('description', vm.description);
    formData.append('pricePerNight', vm.pricePerNight.toString());
    formData.append('location', vm.location);
    formData.append('latitude', vm.latitude.toString());
    formData.append('longitude', vm.longitude.toString());
    formData.append('maxGuests', vm.maxGuests.toString());

    if (vm.amenities && vm.amenities.length > 0) {
      vm.amenities.forEach((amenity: string, index: number) => {
        formData.append(`amenities[${index}]`, amenity);
      });
    }

    if (vm.images && vm.images.length > 0) {
      vm.images.forEach((file: File) => {
        formData.append('images', file);
      });
    }

    if (vm.newImages && vm.newImages.length > 0) {
      vm.newImages.forEach((file: File) => {
        formData.append('newImages', file);
      });
    }

    if (vm.removeImageIds && vm.removeImageIds.length > 0) {
      formData.append('removeImageIds', JSON.stringify(vm.removeImageIds));
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

