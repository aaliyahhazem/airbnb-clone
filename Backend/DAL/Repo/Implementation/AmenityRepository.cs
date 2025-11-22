using DAL.Repo.Implementation;

public class AmenityRepository : GenericRepository<Amenity>, IAmenityRepository
{
    private readonly AppDbContext _context;

    public AmenityRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<Amenity?> GetAmenityWithListingAsync(int keywordId)
    {
        return await _context.Amenities
            .Include(k => k.Listing)
            .FirstOrDefaultAsync(k => k.Id == keywordId);
    }

    public async Task<IEnumerable<Amenity>> SearchAmenitiesAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return Enumerable.Empty<Amenity>();

        return await _context.Amenities
            .Where(k => EF.Functions.Like(k.Word, $"%{searchTerm}%"))
            .ToListAsync();
    }

    public async Task<IEnumerable<Amenity>> GetByListingIdAsync(int listingId)
    {
        return await _context.Amenities
            .Where(k => k.ListingId == listingId)
            .ToListAsync();
    }
}
