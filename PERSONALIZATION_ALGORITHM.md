# User Personalization Algorithm

## Overview
The personalization algorithm intelligently tracks user interactions and preferences to provide a customized browsing experience. It stores preferences in browser's localStorage and uses them to sort listings based on relevance to each individual user.

## How It Works

### 1. **Preference Tracking**
The system automatically tracks user interactions across the platform:

#### Tracked Interactions:
- **Favorites** (Weight: 3x) - When users add listings to favorites
- **Explicit Filters** (Weight: 2x) - When users filter by amenities
- **Search Results** (Weight: 0.5x) - Top 3 results from searches
- **Listing Views** (Weight: 1x) - When users view listing details
- **Filter Changes** (Weight: 1x) - Destination and type filters

#### Data Collected:
- **Amenities**: Frequency count of each amenity from interacted listings
- **Destinations**: Frequency count of each destination
- **Property Types**: Frequency count of each property type
- **Price Ranges**: Last 20 prices from interacted listings

### 2. **Storage Mechanism**
All preferences are stored in browser's localStorage under the key `user_preferences`:

```json
{
  "amenities": {
    "Wi-Fi": 5,
    "Pool": 3,
    "Kitchen": 2
  },
  "destinations": {
    "Cairo": 4,
    "Alexandria": 2
  },
  "types": {
    "Villa": 3,
    "Apartment": 2
  },
  "priceRanges": [1500, 2000, 1800, ...],
  "lastUpdated": 1701619200000
}
```

### 3. **Relevance Scoring Algorithm**

Each listing receives a relevance score (0-100) based on user preferences:

```typescript
Score = (Amenity Score × 50%) + (Destination Score × 25%) + 
        (Type Score × 15%) + (Price Similarity × 10%)
```

#### Component Breakdown:

**Amenity Matching (50 points max)**
- Calculates how many preferred amenities the listing has
- Normalizes against the highest amenity frequency
- Formula: `(matchedAmenityFrequencies / maxFrequency) × 50`

**Destination Matching (25 points max)**
- Checks if listing destination matches user's preferred destinations
- Normalizes against the highest destination frequency
- Formula: `(destinationFrequency / maxDestFrequency) × 25`

**Type Matching (15 points max)**
- Checks if listing type matches user's preferred types
- Normalizes against the highest type frequency
- Formula: `(typeFrequency / maxTypeFrequency) × 15`

**Price Similarity (10 points max)**
- Compares listing price to average of user's viewed prices
- Higher similarity = higher score
- Formula: `max(0, 1 - (|price - avgPrice| / avgPrice)) × 10`

### 4. **Duplicate Prevention**
The system uses **Map** data structures to automatically prevent duplicates:
- Each amenity/destination/type is stored as a unique key
- Repeated interactions increment the frequency count
- No duplicate entries are created

### 5. **Personalized Sorting**

Listings are sorted in the following order:

1. **Calculate Score**: Each listing gets a relevance score
2. **Sort Descending**: Listings with higher scores appear first
3. **Preserve Original Order**: If no preferences exist, original order is maintained

Applied to:
- ✅ Listings Page (filtered results)
- ✅ Home Page (top priority section)
- ✅ Map View (all properties)

## Implementation Details

### Service: `UserPreferencesService`

**Location**: `frontend/src/app/core/services/user-preferences/user-preferences.service.ts`

#### Key Methods:

```typescript
// Track a listing interaction
trackListingInteraction(listing: ListingOverviewVM, weight: number = 1): void

// Track when user favorites a listing (3x weight)
trackFavorite(listing: ListingOverviewVM): void

// Track when user filters by amenities (2x weight)
trackAmenityFilter(amenities: string[]): void

// Calculate relevance score for a listing
calculateRelevanceScore(listing: ListingOverviewVM): number

// Sort listings by relevance
sortByRelevance(listings: ListingOverviewVM[]): ListingOverviewVM[]

// Get top preferred amenities
getTopPreferredAmenities(limit: number = 5): string[]

// Clear all preferences
clearPreferences(): void

// Get statistics for debugging
getPreferenceStats(): any
```

### Integration Points

#### 1. Listings Component
**File**: `frontend/src/app/features/listings-page/listings/listings.ts`

- Tracks amenity filter changes
- Tracks favorite actions
- Tracks search interactions
- Applies personalized sorting to filtered results

#### 2. Listing Service
**File**: `frontend/src/app/core/services/listings/listing.service.ts`

- Tracks when users view listing details
- Automatically records preferences when `getById()` is called

#### 3. Home Component
**File**: `frontend/src/app/features/home-page/home/home.ts`

- Applies personalized sorting to "Top Priority" section

#### 4. Map Component
**File**: `frontend/src/app/features/Map/map/map.ts`

- Applies personalized sorting to map markers
- Most relevant properties appear first in the list

### Visual Indicators

#### Personalization Badge
**Component**: `PersonalizationBadge`
**Location**: `frontend/src/app/shared/components/personalization-badge/personalization-badge.ts`

Displays:
- ⭐ "Personalized" badge when preferences exist
- Tooltip with explanation
- Top 3 preferred amenities

Shows on:
- Listings page header
- Indicates results are being personalized

## User Privacy

### Data Storage
- All data stored **locally** in browser's localStorage
- **No server transmission** of preference data
- User can clear preferences at any time
- Data persists across sessions

### Cookie-like Behavior
While technically using localStorage (not cookies), the behavior is similar:
- Persists across page refreshes
- Isolated per browser/device
- Can be cleared by user
- No cross-site tracking

## Performance Considerations

### Efficiency
- **O(n log n)** sorting complexity
- Calculations done client-side
- No API calls for personalization
- Lazy evaluation (only when needed)

### Memory Usage
- Lightweight JSON storage
- Map structures for fast lookups
- Limited to last 20 price points
- Auto-cleanup of old data

## Testing & Debugging

### View Preferences
Open browser console and run:
```javascript
localStorage.getItem('user_preferences')
```

### Get Statistics
```typescript
userPreferencesService.getPreferenceStats()
// Returns: { totalAmenities, topAmenities, totalDestinations, avgPrice, ... }
```

### Clear Preferences
```typescript
userPreferencesService.clearPreferences()
```

## Future Enhancements

Potential improvements:
1. **Decay Algorithm**: Reduce weight of old preferences over time
2. **Session Analysis**: Track time spent on listings
3. **Collaborative Filtering**: "Users like you also liked..."
4. **A/B Testing**: Measure personalization effectiveness
5. **Export/Import**: Allow users to save/restore preferences
6. **Privacy Controls**: Let users opt-out or manage preferences

## Example User Journey

1. **User visits site** → No preferences, default sorting
2. **Searches "Cairo Villa"** → Top 3 results tracked (0.5x weight each)
3. **Filters by "Pool" amenity** → Pool tracked (2x weight)
4. **Clicks listing with Pool + Wi-Fi** → Both amenities tracked (1x weight)
5. **Favorites the listing** → All amenities tracked (3x weight)
6. **Returns later** → Listings with Pool/Wi-Fi appear first
7. **Views map** → Properties with matching amenities prioritized

## Algorithm Weights Summary

| Action | Weight | Impact |
|--------|--------|--------|
| Favorite | 3.0x | Highest - clear user intent |
| Amenity Filter | 2.0x | High - explicit preference |
| Listing View | 1.0x | Medium - general interest |
| Search Result | 0.5x | Low - exploratory behavior |

## Formula Reference

```
Relevance Score = 
  (Σ(amenityFrequencies) / maxAmenityFreq) × 50 +
  (destinationFrequency / maxDestFreq) × 25 +
  (typeFrequency / maxTypeFreq) × 15 +
  max(0, 1 - |price - avgPrice| / avgPrice) × 10

Where:
- Σ(amenityFrequencies) = sum of frequencies for all matched amenities
- maxAmenityFreq = highest frequency among all tracked amenities
- Similar normalization for destination and type
- Price similarity uses absolute difference from average
```

## Notes

- System gracefully handles missing data
- Works without any user preferences (falls back to default sorting)
- Real-time updates (no page refresh needed)
- Fully reactive with Angular signals
- Compatible with all existing filters and sorting
