
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace DAL.Repo.Implementation
{
    public class MapRepo : IMapRepo
    {
        private readonly AppDbContext _context;

        public MapRepo(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Listing>> GetPropertiesInBoundsAsync(
            double northEastLat,
            double northEastLng,
            double southWestLat,
            double southWestLng

            )
        {
            var query = _context.Listings
                .AsNoTracking()
                .Include(p => p.Location)
                .Include(p => p.Images)
                .Include(p => p.Reviews)
                .Where(p => p.Location != null &&
                           p.Latitude >= southWestLat &&
                           p.Latitude <= northEastLat &&
                           p.Longitude >= southWestLng &&
                           p.Longitude <= northEastLng);

       

            return await query.Take(200).ToListAsync(); // Limit for performance
        }

        public async Task<Listing?> GetPropertyWithLocationAsync(int id)
        {
            return await _context.Listings
                .Include(p => p.Images)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Id == id);
        }
    }
}

