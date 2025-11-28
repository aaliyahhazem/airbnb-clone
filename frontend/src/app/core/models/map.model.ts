export interface MapSearchRequest {
  northEastLat: number;
  northEastLng: number;
  southWestLat: number;
  southWestLng: number;
  minPrice?: number;
  maxPrice?: number;
  minBedrooms?: number;
  checkIn?: string; // ISO string
  checkOut?: string; // ISO string
}

export interface PropertyMap {
  id: number;
  title: string;
  pricePerNight: number;
  latitude: number;
  longitude: number;
  mainImageUrl?: string;
  averageRating?: number;
  reviewCount: number;
}

// GeocodeResponse
export interface GeocodeResponse {
  latitude: number;
  longitude: number;
  formattedAddress: string;
  country: string;
  city: string;
  street: string;
}