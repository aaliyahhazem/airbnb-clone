public interface IAmenityRepository : IGenericRepository<Amenity>
{
    // Get a single keyword including its owning listing
    Task<Amenity?> GetAmenityWithListingAsync(int keywordId);

    // Search keywords by text
    Task<IEnumerable<Amenity>> SearchAmenitiesAsync(string searchTerm);

    // Get all keywords for a listing
    Task<IEnumerable<Amenity>> GetByListingIdAsync(int listingId);
}
