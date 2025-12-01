export interface FavoriteVM {
  id: number;
  userId: string;
  listingId: number;
  createdAt: string;
  listing?: FavoriteListingVM;
}

export interface FavoriteListingVM {
  id: number;
  title: string;
  description: string;
  pricePerNight: number;
  location: string;
  latitude: number;
  longitude: number;
  maxGuests: number;
  isPromoted: boolean;
  mainImageUrl?: string;
  favoriteCount: number;
  destination: string;
  type: string;
  bedrooms: number;
  bathrooms: number;
  averageRating?: number;
  reviewCount: number;
  isApproved: boolean;
}

export interface PaginatedFavoritesVM {
  favorites: FavoriteVM[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export interface AddFavoriteVM {
  listingId: number;
}

export interface FavoriteStatsVM {
  totalFavorites: number;
  uniqueUsers: number;
  uniqueListings: number;
  topListings: TopFavoritedListingVM[];
}

export interface TopFavoritedListingVM {
  listingId: number;
  title: string;
  favoriteCount: number;
}

export interface FavoriteResponse<T> {
  result?: T;
  success?: boolean;
  errorMessage?: string;
  isError?: boolean;
}