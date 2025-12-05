import { Injectable, inject } from '@angular/core';
import { ListingOverviewVM } from '../../models/listing.model';
import { AuthService } from '../auth-service';

interface UserPreferences {
  amenities: Map<string, number>; // amenity -> frequency count
  destinations: Map<string, number>;
  types: Map<string, number>;
  priceRanges: number[]; // Array of prices user has interacted with
  lastUpdated: number;
  userId: string; // Track which user these preferences belong to
}

@Injectable({
  providedIn: 'root'
})
export class UserPreferencesService {
  private readonly STORAGE_KEY_PREFIX = 'user_preferences_';
  private readonly GUEST_USER_ID = 'guest';
  private readonly MAX_AMENITY_SCORE = 100;
  private readonly DECAY_FACTOR = 0.95; // Decay older preferences slightly
  private authService = inject(AuthService);
  private currentUserId: string = this.GUEST_USER_ID;
  private isInitialized = false;

  constructor() {
    // Subscribe to auth state changes FIRST before getting current user
    this.authService.currentUser$.subscribe(user => {
      console.log('🔔 UserPreferencesService: currentUser$ emitted:', user);

      // Extract userId from user object - use userName if available, fallback to email or guest
      const newUserId = user?.userName || user?.email || this.GUEST_USER_ID;
      console.log('📊 Determined userId:', newUserId, 'from user:', { userName: user?.userName, email: user?.email });

      // Only update if user ID actually changed to avoid unnecessary re-initialization
      if (newUserId !== this.currentUserId) {
        console.log('✅ User ID changed from', this.currentUserId, 'to', newUserId);
        this.currentUserId = newUserId;
        this.initializePreferences();
      } else if (!this.isInitialized) {
        // First time initialization even if user hasn't changed
        console.log('🆕 First initialization with userId:', this.currentUserId);
        this.initializePreferences();
      }
    });
  }

  /**
   * Get the storage key for the current user
   */
  private getStorageKey(): string {
    return `${this.STORAGE_KEY_PREFIX}${this.currentUserId}`;
  }

  private initializePreferences(): void {
    this.isInitialized = true;
    const storageKey = this.getStorageKey();
    const stored = localStorage.getItem(storageKey);
    if (!stored) {
      this.savePreferences({
        amenities: new Map(),
        destinations: new Map(),
        types: new Map(),
        priceRanges: [],
        lastUpdated: Date.now(),
        userId: this.currentUserId
      });
    }
  }

  private getPreferences(): UserPreferences {
    const storageKey = this.getStorageKey();
    const stored = localStorage.getItem(storageKey);

    if (!stored) {
      return {
        amenities: new Map(),
        destinations: new Map(),
        types: new Map(),
        priceRanges: [],
        lastUpdated: Date.now(),
        userId: this.currentUserId
      };
    }

    const parsed = JSON.parse(stored);
    return {
      amenities: new Map(Object.entries(parsed.amenities || {})),
      destinations: new Map(Object.entries(parsed.destinations || {})),
      types: new Map(Object.entries(parsed.types || {})),
      priceRanges: parsed.priceRanges || [],
      lastUpdated: parsed.lastUpdated || Date.now(),
      userId: parsed.userId || this.currentUserId
    };
  }

  private savePreferences(preferences: UserPreferences): void {
    const storageKey = this.getStorageKey();
    console.log('💾 Saving preferences to:', storageKey, 'userId:', this.currentUserId);
    const toSave = {
      amenities: Object.fromEntries(preferences.amenities),
      destinations: Object.fromEntries(preferences.destinations),
      types: Object.fromEntries(preferences.types),
      priceRanges: preferences.priceRanges,
      lastUpdated: preferences.lastUpdated,
      userId: preferences.userId
    };
    localStorage.setItem(storageKey, JSON.stringify(toSave));
  }

  /**
   * Track user interaction with a listing (view, favorite, search result)
   */
  trackListingInteraction(listing: ListingOverviewVM, weight: number = 1): void {
    const preferences = this.getPreferences();

    // Track amenities (avoid duplicates, increment count)
    if (listing.amenities && listing.amenities.length > 0) {
      listing.amenities.forEach(amenity => {
        const currentCount = preferences.amenities.get(amenity) || 0;
        preferences.amenities.set(amenity, currentCount + weight);
      });
    }

    // Track destination
    if (listing.destination) {
      const currentCount = preferences.destinations.get(listing.destination) || 0;
      preferences.destinations.set(listing.destination, currentCount + weight);
    }

    // Track property type
    if (listing.type) {
      const currentCount = preferences.types.get(listing.type) || 0;
      preferences.types.set(listing.type, currentCount + weight);
    }

    // Track price range (keep last 20 prices)
    preferences.priceRanges.push(listing.pricePerNight);
    if (preferences.priceRanges.length > 20) {
      preferences.priceRanges.shift();
    }

    preferences.lastUpdated = Date.now();
    this.savePreferences(preferences);
  }

  /**
   * Track when user favorites a listing (higher weight)
   */
  trackFavorite(listing: ListingOverviewVM): void {
    this.trackListingInteraction(listing, 3); // 3x weight for favorites
  }

  /**
   * Track when user searches/filters by amenities
   */
  trackAmenityFilter(amenities: string[]): void {
    if (amenities.length === 0) return;

    const preferences = this.getPreferences();
    amenities.forEach(amenity => {
      const currentCount = preferences.amenities.get(amenity) || 0;
      preferences.amenities.set(amenity, currentCount + 2); // 2x weight for explicit filter
    });

    preferences.lastUpdated = Date.now();
    this.savePreferences(preferences);
  }

  /**
   * Calculate relevance score for a listing based on user preferences
   */
  calculateRelevanceScore(listing: ListingOverviewVM): number {
    const preferences = this.getPreferences();
    let score = 0;

    // Amenity matching (highest weight)
    if (listing.amenities && listing.amenities.length > 0) {
      const amenityScore = listing.amenities.reduce((sum, amenity) => {
        const frequency = preferences.amenities.get(amenity) || 0;
        return sum + frequency;
      }, 0);

      // Normalize amenity score (0-50 points)
      const maxAmenityFrequency = Math.max(...Array.from(preferences.amenities.values()), 1);
      score += (amenityScore / maxAmenityFrequency) * 50;
    }

    // Destination matching (medium weight)
    if (listing.destination) {
      const destFrequency = preferences.destinations.get(listing.destination) || 0;
      const maxDestFrequency = Math.max(...Array.from(preferences.destinations.values()), 1);
      score += (destFrequency / maxDestFrequency) * 25;
    }

    // Type matching (medium weight)
    if (listing.type) {
      const typeFrequency = preferences.types.get(listing.type) || 0;
      const maxTypeFrequency = Math.max(...Array.from(preferences.types.values()), 1);
      score += (typeFrequency / maxTypeFrequency) * 15;
    }

    // Price similarity (low weight)
    if (preferences.priceRanges.length > 0) {
      const avgPrice = preferences.priceRanges.reduce((a, b) => a + b, 0) / preferences.priceRanges.length;
      const priceDiff = Math.abs(listing.pricePerNight - avgPrice);
      const maxPriceDiff = avgPrice; // Use average as baseline
      const priceSimilarity = Math.max(0, 1 - (priceDiff / maxPriceDiff));
      score += priceSimilarity * 10;
    }

    return score;
  }

  /**
   * Sort listings by relevance score (personalized ranking)
   */
  sortByRelevance(listings: ListingOverviewVM[]): ListingOverviewVM[] {
    const preferences = this.getPreferences();

    // If no preferences yet, return original order
    if (preferences.amenities.size === 0 &&
        preferences.destinations.size === 0 &&
        preferences.types.size === 0) {
      return listings;
    }

    // Calculate score for each listing and sort
    const scoredListings = listings.map(listing => ({
      listing,
      score: this.calculateRelevanceScore(listing)
    }));

    // Sort by score descending (highest relevance first)
    scoredListings.sort((a, b) => b.score - a.score);

    return scoredListings.map(item => item.listing);
  }

  /**
   * Get top preferred amenities for display/suggestions
   */
  getTopPreferredAmenities(limit: number = 5): string[] {
    const preferences = this.getPreferences();
    const sorted = Array.from(preferences.amenities.entries())
      .sort((a, b) => b[1] - a[1])
      .slice(0, limit)
      .map(entry => entry[0]);
    return sorted;
  }

  /**
   * Clear all preferences (reset)
   */
  clearPreferences(): void {
    const storageKey = this.getStorageKey();
    localStorage.removeItem(storageKey);
    this.initializePreferences();
  }

  /**
   * Migrate guest preferences to logged-in user
   * Call this after user login to preserve browsing history
   */
  migrateGuestPreferences(userId: string): void {
    const guestKey = `${this.STORAGE_KEY_PREFIX}${this.GUEST_USER_ID}`;
    const userKey = `${this.STORAGE_KEY_PREFIX}${userId}`;

    // Get guest preferences
    const guestData = localStorage.getItem(guestKey);
    if (!guestData) return;

    // Check if user already has preferences
    const userData = localStorage.getItem(userKey);

    if (!userData) {
      // User has no preferences, migrate guest preferences
      const parsed = JSON.parse(guestData);
      parsed.userId = userId;
      localStorage.setItem(userKey, JSON.stringify(parsed));
    } else {
      // User has preferences, merge with guest preferences (give priority to user's existing preferences)
      const guestPrefs = JSON.parse(guestData);
      const userPrefs = JSON.parse(userData);

      // Merge amenities (add guest counts to user counts)
      const mergedAmenities = { ...guestPrefs.amenities };
      Object.entries(userPrefs.amenities || {}).forEach(([amenity, count]) => {
        mergedAmenities[amenity] = (mergedAmenities[amenity] || 0) + (count as number);
      });

      // Merge destinations
      const mergedDestinations = { ...guestPrefs.destinations };
      Object.entries(userPrefs.destinations || {}).forEach(([dest, count]) => {
        mergedDestinations[dest] = (mergedDestinations[dest] || 0) + (count as number);
      });

      // Merge types
      const mergedTypes = { ...guestPrefs.types };
      Object.entries(userPrefs.types || {}).forEach(([type, count]) => {
        mergedTypes[type] = (mergedTypes[type] || 0) + (count as number);
      });

      // Merge price ranges (combine and keep last 20)
      const mergedPriceRanges = [
        ...(guestPrefs.priceRanges || []),
        ...(userPrefs.priceRanges || [])
      ].slice(-20);

      // Save merged preferences
      const merged = {
        amenities: mergedAmenities,
        destinations: mergedDestinations,
        types: mergedTypes,
        priceRanges: mergedPriceRanges,
        lastUpdated: Date.now(),
        userId: userId
      };
      localStorage.setItem(userKey, JSON.stringify(merged));
    }

    // Remove guest preferences after migration
    localStorage.removeItem(guestKey);
  }

  /**
   * Get preference statistics for debugging
   */
  getPreferenceStats(): any {
    const preferences = this.getPreferences();
    return {
      totalAmenities: preferences.amenities.size,
      topAmenities: this.getTopPreferredAmenities(10),
      totalDestinations: preferences.destinations.size,
      totalTypes: preferences.types.size,
      avgPrice: preferences.priceRanges.length > 0
        ? preferences.priceRanges.reduce((a, b) => a + b, 0) / preferences.priceRanges.length
        : 0,
      lastUpdated: new Date(preferences.lastUpdated).toISOString()
    };
  }
}
