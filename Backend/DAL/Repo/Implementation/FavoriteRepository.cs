using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repo.Implementation
{
    public class FavoriteRepository : GenericRepository<Favorite>, IFavoriteRepository
    {
        public FavoriteRepository(AppDbContext context) : base(context) { }
        // Count how many users favorited a listing
        public async Task<int> CountListingFavoritesAsync(int listingId)
        {
            return await _context.Favorites.Where(f => f.ListingId == listingId).CountAsync();
        }
        // Count total favorites for a user
        public async Task<int> CountUserFavoritesAsync(Guid userId)
        {
            return await _context.Favorites.Where(f => f.UserId == userId).CountAsync();
        }
        // Delete all favorites for a user
        public async Task<int> DeleteAllUserFavoritesAsync(Guid userId)
        {
           var favorites = await _context.Favorites.Where(f => f.UserId == userId).ToListAsync();
            _context.Favorites.RemoveRange(favorites);
            return await _context.SaveChangesAsync(); 
        }
        // Get a specific favorite by user and listing
        public async Task<Favorite?> GetByUserAndListingAsync(Guid userId, int listingId)
        {
            return await _context.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.ListingId == listingId);
        }
        // Get all users who favorited a specific listing
        public async Task<IEnumerable<Favorite>> GetListingFavoritesAsync(int listingId)
        {
            return await _context.Favorites
                                .Where(f => f.ListingId == listingId)
                                .Include(f => f.User)
                                .OrderByDescending(f => f.CreatedAt)
                                .AsNoTracking()
                                .ToListAsync();
        }
        // Get most favorited listings
        public async Task<IEnumerable<Listing>> GetMostFavoritedListingsAsync(int count = 10)
        {
            var listingIds = await _context.Favorites
                                           .GroupBy(f => f.ListingId)
                                           .OrderByDescending(g => g.Count())
                                           .Take(count)
                                           .Select(g => g.Key)
                                           .ToListAsync();

            return await _context.Listings
                                 .Where(l => listingIds.Contains(l.Id))
                                 .Include(l => l.Images)
                                 .Include(l => l.MainImage)
                                 .AsNoTracking()
                                 .ToListAsync();
        }
        // Get listing IDs that a user has favorited
        public async Task<List<int>> GetUserFavoriteListingIdsAsync(Guid userId)
        {
            return await _context.Favorites
                                 .Where(f => f.UserId == userId)
                                 .Select(f => f.ListingId)
                                 .ToListAsync();
        }
        // Get all favorites for a specific user with listing details
        public async Task<IEnumerable<Favorite>> GetUserFavoritesAsync(Guid userId)
        {
            return await _context.Favorites
                                 .Where(f => f.UserId == userId)
                                 .Include(f => f.Listing)
                                 .ThenInclude(l => l.Images)
                                 .Include(f => f.Listing)
                                 .ThenInclude(l => l.MainImage)
                                 .OrderByDescending(f => f.CreatedAt)
                                 .AsNoTracking()
                                 .ToListAsync();
        }
        // Get paginated favorites for a user
        public async Task<IEnumerable<Favorite>> GetUserFavoritesPaginatedAsync(Guid userId, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            return await _context.Favorites
                                 .Where(f => f.UserId == userId)
                                 .Include(f => f.Listing)
                                 .ThenInclude(l => l.Images)
                                 .Include(f => f.Listing)
                                 .ThenInclude(l => l.MainImage)
                                 .OrderByDescending(f => f.CreatedAt)
                                 .Skip((page - 1) * pageSize)
                                 .Take(pageSize)
                                 .AsNoTracking()
                                 .ToListAsync();
        }
        // Check if a user has favorited a specific listing
        public async Task<bool> IsFavoritedByUserAsync(Guid userId, int listingId)
        {
            return await _context.Favorites.AnyAsync(f => f.UserId == userId && f.ListingId == listingId);
        }
    }
}
