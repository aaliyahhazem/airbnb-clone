using BLL.ModelVM.Favorite;
using BLL.Services.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services.Impelementation
{
    public class FavoriteService : IFavoriteService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;

        public FavoriteService(
            IUnitOfWork uow,
            IMapper mapper,
            INotificationService notificationService)
        {
            _uow = uow;
            _mapper = mapper;
            _notificationService = notificationService;
        }
        public async Task<Response<FavoriteVM>> AddFavoriteAsync(Guid userId, int listingId)
        {
            try
            {
                // Check if listing exists
                var listing = await _uow.Listings.GetByIdAsync(listingId);
                if (listing == null)
                    return Response<FavoriteVM>.FailResponse("Listing not found");

                // Check if already favorited
                var existing = await _uow.Favorites.GetByUserAndListingAsync(userId, listingId);
                if (existing != null)
                    return Response<FavoriteVM>.FailResponse("Listing is already in your favorites");

                // Create favorite
                var favorite = Favorite.Create(userId, listingId);
                await _uow.Favorites.AddAsync(favorite);
                await _uow.SaveChangesAsync();

                //listing details
                var created = await _uow.Favorites.GetByUserAndListingAsync(userId, listingId);
                var mappedFavorites = _mapper.Map<FavoriteVM>(created!);
                // Send notification to host
                await _notificationService.CreateAsync(new CreateNotificationVM
                {
                    UserId = listing.UserId,
                    Title = "New Favorite",
                    Body = $"Someone added your listing \"{listing.Title}\" to favorites!",
                    Type = NotificationType.System,
                    CreatedAt = DateTime.UtcNow
                });
                return Response<FavoriteVM>.SuccessResponse(mappedFavorites);
            }
            catch (Exception ex)
            {
                return Response<FavoriteVM>.FailResponse($"Error adding favorite: {ex.Message}");
            }
        }

        public async Task<Response<Dictionary<int, bool>>> BatchCheckFavoritesAsync(Guid userId, List<int> listingIds)
        {
            try
            {
                var favoriteIds = await _uow.Favorites.GetUserFavoriteListingIdsAsync(userId);
                var results = listingIds.ToDictionary(
                    id => id,
                    id => favoriteIds.Contains(id)
                );

                return Response<Dictionary<int, bool>>.SuccessResponse(results);
            }
            catch (Exception ex)
            {
                return Response<Dictionary<int, bool>>.FailResponse($"Error checking favorites: {ex.Message}");
            }
        }

        public async Task<Response<bool>> ClearAllFavoritesAsync(Guid userId)
        {
            try
            {
                var deleted = await _uow.Favorites.DeleteAllUserFavoritesAsync(userId);
                return Response<bool>.SuccessResponse(true);
            }
            catch (Exception ex)
            {
                return Response<bool>.FailResponse($"Error clearing favorites: {ex.Message}");
            }
        }

        public async Task<Response<FavoriteStatsVM>> GetFavoriteStatsAsync()
        {
            try
            {
                var allFavorites = await _uow.Favorites.GetAllAsync();

                var stats = new FavoriteStatsVM
                {
                    TotalFavorites = allFavorites.Count(),
                    UniqueUsers = allFavorites.Select(f => f.UserId).Distinct().Count(),
                    UniqueListings = allFavorites.Select(f => f.ListingId).Distinct().Count(),
                    TopListings = allFavorites
                        .GroupBy(f => new { f.ListingId, f.Listing.Title })
                        .OrderByDescending(g => g.Count())
                        .Take(10)
                        .Select(g => new TopFavoritedListingVM
                        {
                            ListingId = g.Key.ListingId,
                            Title = g.Key.Title,
                            FavoriteCount = g.Count()
                        })
                        .ToList()
                };

                return Response<FavoriteStatsVM>.SuccessResponse(stats);
            }
            catch (Exception ex)
            {
                return Response<FavoriteStatsVM>.FailResponse($"Error retrieving stats: {ex.Message}");
            }
        }

        public async Task<Response<int>> GetListingFavoritesCountAsync(int listingId)
        {
            try
            {
                var count = await _uow.Favorites.CountListingFavoritesAsync(listingId);
                return Response<int>.SuccessResponse(count);
            }
            catch (Exception ex)
            {
                return Response<int>.FailResponse($"Error counting favorites: {ex.Message}");
            }
        }
        public async Task<Response<List<FavoriteListingVM>>> GetMostFavoritedListingsAsync(int count = 10)
        {
            try
            {
                var listings = await _uow.Favorites.GetMostFavoritedListingsAsync(count);
                var vms = new List<FavoriteListingVM>();

                foreach (var listing in listings)
                {
                    var favoriteCount = await _uow.Favorites.CountListingFavoritesAsync(listing.Id);
                    var vm = _mapper.Map<FavoriteListingVM>(listing);
                    vm.FavoriteCount = favoriteCount;
                    vms.Add(vm);
                }

                return Response<List<FavoriteListingVM>>.SuccessResponse(vms);
            }
            catch (Exception ex)
            {
                return Response<List<FavoriteListingVM>>.FailResponse($"Error retrieving popular listings: {ex.Message}");
            }
        }

        public async Task<Response<List<FavoriteVM>>> GetUserFavoritesAsync(Guid userId)
        {
            try
            {
                var favorites = await _uow.Favorites.GetUserFavoritesAsync(userId);
                var mappedFavorites = _mapper.Map<List<FavoriteVM>>(favorites);
                return Response<List<FavoriteVM>>.SuccessResponse(mappedFavorites);
            }
            catch (Exception ex)
            {
                return Response<List<FavoriteVM>>.FailResponse($"Error retrieving favorites: {ex.Message}");
            }
        }

        public async Task<Response<int>> GetUserFavoritesCountAsync(Guid userId)
        {
            try
            {
                var count = await _uow.Favorites.CountUserFavoritesAsync(userId);
                return Response<int>.SuccessResponse(count);
            }
            catch (Exception ex)
            {
                return Response<int>.FailResponse($"Error counting favorites: {ex.Message}");
            }
        }

        public async Task<Response<PaginatedFavoritesVM>> GetUserFavoritesPaginatedAsync(Guid userId, int page = 1, int pageSize = 10)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var favorites = await _uow.Favorites.GetUserFavoritesPaginatedAsync(userId, page, pageSize);
                var totalCount = await _uow.Favorites.CountUserFavoritesAsync(userId);

                var result = new PaginatedFavoritesVM
                {
                    Favorites = _mapper.Map<List<FavoriteVM>>(favorites),
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };

                return Response<PaginatedFavoritesVM>.SuccessResponse(result);
            }
            catch (Exception ex)
            {
                return Response<PaginatedFavoritesVM>.FailResponse($"Error retrieving favorites: {ex.Message}");
            }
        }

        public async Task<Response<bool>> IsFavoritedAsync(Guid userId, int listingId)
        {
            try
            {
                var isFavorited = await _uow.Favorites.IsFavoritedByUserAsync(userId, listingId);
                return Response<bool>.SuccessResponse(isFavorited);
            }
            catch (Exception ex)
            {
                return Response<bool>.FailResponse($"Error checking favorite: {ex.Message}");
            }
        }

        public async Task<Response<bool>> RemoveFavoriteAsync(Guid userId, int listingId)
        {
            try
            {
                var favorite = await _uow.Favorites.GetByUserAndListingAsync(userId, listingId);
                if (favorite == null)
                    return Response<bool>.FailResponse("Favorite not found");

                //Check if it belongs to user
                if (!favorite.BelongsToUser(userId))
                    return Response<bool>.FailResponse("Unauthorized to remove this favorite");
                _uow.Favorites.Delete(favorite);
                await _uow.SaveChangesAsync();

                return Response<bool>.SuccessResponse(true);
            }
            catch (Exception ex)
            {
                return Response<bool>.FailResponse($"Error removing favorite: {ex.Message}");
            }
        }
        public async Task<Response<bool>> ToggleFavoriteAsync(Guid userId, int listingId)
        {
            try
            {
                var isFavorited = await _uow.Favorites.IsFavoritedByUserAsync(userId, listingId);

                if (isFavorited)
                {
                    var remove = await RemoveFavoriteAsync(userId, listingId);
                    return Response<bool>.SuccessResponse(false);
                }
                else
                {
                    var add = await AddFavoriteAsync(userId, listingId);
                    if (!add.Success)
                        return Response<bool>.FailResponse(add.errorMessage);

                    return Response<bool>.SuccessResponse(true);
                }
            }
            catch (Exception ex)
            {
                return Response<bool>.FailResponse($"Error toggling favorite: {ex.Message}");
            }
        }
    }
}
