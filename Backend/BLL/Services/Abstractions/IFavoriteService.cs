using BLL.ModelVM.Favorite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services.Abstractions
{
    public interface IFavoriteService
    {
        Task<Response<FavoriteVM>> AddFavoriteAsync(Guid userId, int listingId);
        Task<Response<bool>> RemoveFavoriteAsync(Guid userId, int listingId);
        Task<Response<bool>> ToggleFavoriteAsync(Guid userId, int listingId);
        Task<Response<List<FavoriteVM>>> GetUserFavoritesAsync(Guid userId);
        Task<Response<PaginatedFavoritesVM>> GetUserFavoritesPaginatedAsync(Guid userId, int page = 1, int pageSize = 10);
        Task<Response<bool>> IsFavoritedAsync(Guid userId, int listingId);
        Task<Response<Dictionary<int, bool>>> BatchCheckFavoritesAsync(Guid userId, List<int> listingIds);
        Task<Response<int>> GetUserFavoritesCountAsync(Guid userId);
        Task<Response<int>> GetListingFavoritesCountAsync(int listingId);
        Task<Response<List<FavoriteListingVM>>> GetMostFavoritedListingsAsync(int count = 10);
        Task<Response<bool>> ClearAllFavoritesAsync(Guid userId);
        Task<Response<FavoriteStatsVM>> GetFavoriteStatsAsync();



    }
}
