using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repo.Abstraction
{
    public interface IFavoriteRepository : IGenericRepository<Favorite>
    {
        // Get all favorites for a specific user with listing details
        Task<IEnumerable<Favorite>> GetUserFavoritesAsync(Guid userId);
        // Get paginated favorites for a user
        Task<IEnumerable<Favorite>> GetUserFavoritesPaginatedAsync(Guid userId,int page,int pageSize);
        // Count total favorites for a user
        Task<int> CountUserFavoritesAsync(Guid userId);
        // Check if a user has favorited a specific listing
        Task<bool> IsFavoritedByUserAsync(Guid userId, int listingId);
        // Get a specific favorite by user and listing
        Task<Favorite?> GetByUserAndListingAsync(Guid userId, int listingId);
        // Get all users who favorited a specific listing
        Task<IEnumerable<Favorite>> GetListingFavoritesAsync(int listingId);
        // Count how many users favorited a listing
        Task<int> CountListingFavoritesAsync(int listingId);
        // Get most favorited listings
        Task<IEnumerable<Listing>> GetMostFavoritedListingsAsync(int count = 10);
        // Delete all favorites for a user
        Task<int> DeleteAllUserFavoritesAsync(Guid userId);
        // Get listing IDs that a user has favorited
        Task<List<int>> GetUserFavoriteListingIdsAsync(Guid userId);



    }
}
