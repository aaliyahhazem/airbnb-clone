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
  description?: string;
  pricePerNight: number;
  latitude: number;
  longitude: number;
  location?: string;
  destination?: string;
  mainImageUrl?: string;
  type: string;
  bedrooms: number;
  bathrooms: number;
  averageRating?: number;
  reviewCount: number;
  amenities?: string[];
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
