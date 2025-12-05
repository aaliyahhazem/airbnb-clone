using BLL.ModelVM.Favorite;
using BLL.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<FavoriteService> _logger;

        public FavoriteService(
            IUnitOfWork uow,
            IMapper mapper,
            INotificationService notificationService,
            ILogger<FavoriteService> logger)
        {
            _uow = uow;
            _mapper = mapper;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<Response<FavoriteVM>> AddFavoriteAsync(Guid userId, int listingId)
        {
            if (userId == Guid.Empty)
            {
                _logger.LogWarning("AddFavoriteAsync called with empty userId");
                return Response<FavoriteVM>.FailResponse("User is not authenticated.");
            }

            try
            {
                // Verify listing exists first
                var listing = await _uow.Listings.GetByIdAsync(listingId);
                if (listing == null)
                {
                    _logger.LogWarning("Listing {ListingId} not found when adding favorite for user {UserId}", listingId, userId);
                    return Response<FavoriteVM>.FailResponse("Listing not found");
                }

                // Check if already favorited
                var existing = await _uow.Favorites.GetByUserAndListingAsync(userId, listingId);
                if (existing != null)
                {
                    _logger.LogInformation("User {UserId} already favorited listing {ListingId}", userId, listingId);
                    return Response<FavoriteVM>.FailResponse("Listing is already in your favorites");
                }

                // Create favorite
                var favorite = Favorite.Create(userId, listingId);
                await _uow.Favorites.AddAsync(favorite);

                // Save the favorite
                var savedCount = await _uow.SaveChangesAsync();
                _logger.LogInformation("Favorite saved for user {UserId}, listing {ListingId}. Rows affected: {Count}", userId, listingId, savedCount);

                // Increment favorite priority (has its own SaveChanges)
                var priorityResult = await _uow.Listings.IncrementFavoritePriorityAsync(listingId);
                if (!priorityResult)
                {
                    _logger.LogWarning("Failed to increment priority for listing {ListingId}", listingId);
                }

                // Get the created favorite with listing details
                var created = await _uow.Favorites.GetByUserAndListingAsync(userId, listingId);
                if (created == null)
                {
                    _logger.LogError("Favorite was saved but could not be retrieved for user {UserId}, listing {ListingId}", userId, listingId);
                    return Response<FavoriteVM>.FailResponse("Favorite created but could not be retrieved");
                }

                var mappedFavorites = _mapper.Map<FavoriteVM>(created);

                // Send notification asynchronously (fire and forget)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _notificationService.CreateAsync(new CreateNotificationVM
                        {
                            UserId = listing.UserId,
                            Title = "New Favorite",
                            Body = $"Someone added your listing \"{listing.Title}\" to favorites!",
                            Type = NotificationType.System,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send notification for favorite on listing {ListingId}", listingId);
                    }
                });

                return Response<FavoriteVM>.SuccessResponse(mappedFavorites);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "DbUpdateException adding favorite for user {UserId}, listing {ListingId}. Inner: {Inner}",
                    userId, listingId, dbEx.InnerException?.Message ?? "No inner exception");

                // Check for specific SQL errors
                var innerMsg = dbEx.InnerException?.Message ?? dbEx.Message;

                if (innerMsg.Contains("IX_Favorites_UserId_ListingId") || innerMsg.Contains("duplicate"))
                {
                    return Response<FavoriteVM>.FailResponse("This listing is already in your favorites.");
                }

                if (innerMsg.Contains("REFERENCE constraint") || innerMsg.Contains("FOREIGN KEY"))
                {
                    if (innerMsg.Contains("FK_Favorites_Listings"))
                    {
                        return Response<FavoriteVM>.FailResponse("The listing you're trying to favorite doesn't exist or has been deleted.");
                    }
                    if (innerMsg.Contains("FK_Favorites_Users"))
                    {
                        return Response<FavoriteVM>.FailResponse("User account issue. Please log out and log in again.");
                    }
                    return Response<FavoriteVM>.FailResponse("Invalid reference in database.");
                }

                return Response<FavoriteVM>.FailResponse($"Database error while adding favorite: {innerMsg}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error adding favorite for user {UserId}, listing {ListingId}", userId, listingId);
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

                // Decrement favorite priority when removing
                await _uow.Listings.DecrementFavoritePriorityAsync(listingId);

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
