import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class MapService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5235/api/Map';
  private backendOrigin = 'http://localhost:5235';

  private normalizeImageUrl(url?: string): string | undefined {
    if (!url) return undefined;
    const u = String(url);
    if (u.startsWith('http://') || u.startsWith('https://')) return u;
    if (u.startsWith('/')) return `${this.backendOrigin}${u}`;
    return `${this.backendOrigin}/${u}`;
  }

  getProperties(bounds: any) {
    return this.http.get<any>(`${this.apiUrl}/properties`, {
      params: bounds,
    }).pipe(
      map(response => ({
        properties: (response.properties || response.Properties || []).map((p: any) => ({
          id: p.id || p.Id,
          title: p.title || p.Title,
          pricePerNight: p.pricePerNight || p.PricePerNight,
          latitude: p.latitude || p.Latitude,
          longitude: p.longitude || p.Longitude,
          mainImageUrl: p.mainImageUrl || p.MainImageUrl,
          averageRating: p.averageRating || p.AverageRating,
          reviewCount: p.reviewCount || p.ReviewCount
        }))
      }))
    );
  }

  getProperty(id: number) {
    return this.http.get<any>(`${this.apiUrl}/properties/${id}`).pipe(
      map(response => ({
        id: response.id || response.Id || response.result?.id,
        title: response.title || response.Title || response.result?.title,
        pricePerNight: response.pricePerNight || response.PricePerNight || response.result?.pricePerNight,
        latitude: response.latitude || response.Latitude || response.result?.latitude,
        longitude: response.longitude || response.Longitude || response.result?.longitude,
        averageRating: response.averageRating || response.AverageRating || response.result?.averageRating,
        reviewCount: response.reviewCount || response.ReviewCount || response.result?.reviewCount,
        description: response.description || response.Description || response.result?.description
      }))
    );
  }

  geocode(latitude: number, longitude: number) {
    // This endpoint on the backend expects an address string (geocode by address).
    return this.http.post<any>(`${this.apiUrl}/geocode`, { address: `${latitude},${longitude}` }).pipe(
      map(response => ({
        latitude: response.latitude || response.Latitude || response.result?.latitude,
        longitude: response.longitude || response.Longitude || response.result?.longitude,
        formattedAddress: response.formattedAddress || response.FormattedAddress || response.result?.formattedAddress || response.result?.address,
        country: response.country || response.Country || response.result?.country,
        city: response.city || response.City || response.result?.city,
        street: response.street || response.Street || response.result?.street
      }))
    );
  }

  // Reverse geocode using Nominatim (OpenStreetMap). This runs in the frontend and does not require backend support.
  reverseGeocode(latitude: number, longitude: number) {
    const url = `https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat=${encodeURIComponent(
      String(latitude)
    )}&lon=${encodeURIComponent(String(longitude))}`;
    return this.http.get<any>(url).pipe(
      map((resp) => ({
        formattedAddress: resp.display_name,
        city: resp.address?.city || resp.address?.town || resp.address?.village || resp.address?.municipality,
        country: resp.address?.country,
        street: resp.address?.road || resp.address?.pedestrian || resp.address?.neighbourhood,
        latitude: resp.lat ? Number(resp.lat) : latitude,
        longitude: resp.lon ? Number(resp.lon) : longitude
      }))
    );
  }
}