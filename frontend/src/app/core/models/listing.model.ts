// src/app/models/listing.model.ts

// Restrict property types
export type PropertyType = 'Apartment' | 'House' | 'Villa' | 'Studio' | 'Penthouse' | 'Cottage' | 'Chalet' | 'Loft' | 'Cabin' | 'Farmhouse';

// Restrict amenities
export type Amenity =
  | 'Wi-Fi' | 'Pool' | 'AC' | 'Kitchen' | 'Washer' | 'Dryer' | 'TV'
  | 'Heating' | 'Parking' | 'Fireplace' | 'Gym' | 'Breakfast'
  | 'Pets Allowed' | 'Hot Tub' | 'Elevator';

export interface Listing {
  id: number;
  title: string;
  description: string;
  pricePerNight: number;
  location: string;
  latitude: number;
  longitude: number;
  maxGuests: number;
  isApproved: boolean;
  isReviewed: boolean;
  mainImageUrl?: string;
  averageRating?: number;
  reviewCount: number;
  createdAt: string;
  userId: string;
  images?: ListingImage[];
  amenities?: string[];
  destination: string;
  type: string;
  bedrooms: number;
  bathrooms: number;
}

export interface ListingImage {
  id: number;
  imageUrl: string;
  isMainImage: boolean;
}

export interface ListingCreateVM {
  title: string;
  description: string;
  pricePerNight: number;
  location: string;
  latitude: number;
  longitude: number;
  maxGuests: number;
  images?: File[];
  amenities?: string[];
  destination: string;
  type: string;
  numberOfRooms: number;
  numberOfBathrooms: number;
}

export interface ListingUpdateVM {
  title?: string;
  description?: string;
  pricePerNight?: number;
  location?: string;
  latitude?: number;
  longitude?: number;
  maxGuests?: number;
  newImages?: File[];
  removeImageIds?: number[];
  amenities?: string[];
  destination?: string;
  type?: string;
  numberOfRooms?: number;
  numberOfBathrooms?: number;
}

export interface ListingDetailVM {
  id: number;
  title: string;
  description: string;
  pricePerNight: number;
  location: string;
  latitude: number;
  longitude: number;
  maxGuests: number;
  isApproved: boolean;
  mainImageUrl?: string;
  averageRating?: number;
  reviewCount: number;
  images: ListingImage[];
  amenities: string[];
  userId: string;
  createdAt: string;
  destination: string;
  type: string;
  bedrooms: number;
  bathrooms: number;
  // Host Information
  hostId: string;
  hostName: string;
}

export interface ListingOverviewVM {
  id: number;
  title: string;
  pricePerNight: number;
  location: string;
  mainImageUrl?: string;
  averageRating?: number;
  reviewCount: number;
  isApproved: boolean;
  description?: string;
  destination: string;  
  type: string;
  bedrooms: number;
  bathrooms: number;
  createdAt: string;
  // Dynamic Priority & Engagement System
  priority: number;
  viewCount: number;
  favoriteCount: number;
  bookingCount: number;
  amenities?: string[];
}

export interface ListingsResponse<T> {
  data: T;
  message?: string;
  isError: boolean;
}

export interface ListingsPagedResponse<T> {
  data: T[];
  totalCount: number;
  message?: string;
  isError: boolean;
}
