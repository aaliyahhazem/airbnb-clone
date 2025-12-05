using BLL.ModelVM.Favorite;
using BLL.ModelVM.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace PL.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FavoriteController : BaseController
    {
        private readonly IFavoriteService _favoriteService;
        private readonly ILogger<FavoriteController> _logger;

        public FavoriteController(IFavoriteService favoriteService, ILogger<FavoriteController> logger)
        {
            _favoriteService = favoriteService;
            _logger = logger;
        }

        //Add New Favorite
        [HttpPost]
        [ProducesResponseType(typeof(Response<FavoriteVM>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> AddFavorite([FromBody] AddFavoriteVM model)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
            {
                _logger.LogWarning("AddFavorite called but user ID not found in claims");
                return Unauthorized(new { error = "User not authenticated" });
            }

            var result = await _favoriteService.AddFavoriteAsync(userId.Value, model.ListingId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        //Remove Favorite
        [HttpDelete("{listingId:int}")]
        [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveFavorite(int listingId)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
            {
                _logger.LogWarning("RemoveFavorite called but user ID not found in claims");
                return Unauthorized(new { error = "User not authenticated" });
            }

            var result = await _favoriteService.RemoveFavoriteAsync(userId.Value, listingId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        //Toggle Favorite
        [HttpPost("toggle/{listingId:int}")]
        [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ToggleFavorite(int listingId)
        {
            try
            {
                var userId = GetUserIdFromClaims();
                if (userId == null)
                {
                    _logger.LogWarning("ToggleFavorite called but user ID not found in claims");
                    return Unauthorized(new { error = "User not authenticated" });
                }

                _logger.LogInformation("Toggle favorite request: UserId={UserId}, ListingId={ListingId}", userId.Value, listingId);

                var result = await _favoriteService.ToggleFavoriteAsync(userId.Value, listingId);

                if (!result.Success)
                {
                    _logger.LogWarning("Toggle favorite failed: {Error}", result.errorMessage);
                    return BadRequest(result);
                }

                return Ok(new
                {
                    isFavorited = result.result,
                    message = result.result ? "Added to favorites" : "Removed from favorites"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in ToggleFavorite for listing {ListingId}", listingId);
                return BadRequest(new { error = ex.Message });
            }
        }

        //Get My Favorites
        [HttpGet("me")]
        [ProducesResponseType(typeof(Response<List<FavoriteVM>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyFavorites()
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
                return Unauthorized(new { error = "User not authenticated" });

            var result = await _favoriteService.GetUserFavoritesAsync(userId.Value);
            return Ok(result);
        }

        //Get My Favorites Paginated
        [HttpGet("me/paginated")]
        [ProducesResponseType(typeof(Response<PaginatedFavoritesVM>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyFavoritesPaginated(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
                return Unauthorized(new { error = "User not authenticated" });

            var result = await _favoriteService.GetUserFavoritesPaginatedAsync(userId.Value, page, pageSize);
            return Ok(result);
        }

        //Check if Listing is Favorited
        [HttpGet("check/{listingId:int}")]
        [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CheckIsFavorited(int listingId)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
                return Unauthorized(new { error = "User not authenticated" });

            var result = await _favoriteService.IsFavoritedAsync(userId.Value, listingId);
            return Ok(result);
        }

        //Batch Check Favorites
        [HttpPost("check/batch")]
        [ProducesResponseType(typeof(Response<Dictionary<int, bool>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> BatchCheckFavorites([FromBody] BatchFavoriteCheckVM model)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
                return Unauthorized(new { error = "User not authenticated" });

            var result = await _favoriteService.BatchCheckFavoritesAsync(userId.Value, model.ListingIds);
            return Ok(result);
        }

        //Get My Favorites Count
        [HttpGet("me/count")]
        [ProducesResponseType(typeof(Response<int>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyFavoritesCount()
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
                return Unauthorized(new { error = "User not authenticated" });

            var result = await _favoriteService.GetUserFavoritesCountAsync(userId.Value);
            return Ok(result);
        }

        //Get Listing Favorites Count
        [HttpGet("listing/{listingId:int}/count")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Response<int>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListingFavoritesCount(int listingId)
        {
            var result = await _favoriteService.GetListingFavoritesCountAsync(listingId);
            return Ok(result);
        }

        //Get Trending Listings
        [HttpGet("trending")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Response<List<FavoriteListingVM>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTrendingListings([FromQuery] int count = 10)
        {
            if (count < 1 || count > 50) count = 10;

            var result = await _favoriteService.GetMostFavoritedListingsAsync(count);
            return Ok(result);
        }

        //Clear All Favorites
        [HttpDelete("me/clear")]
        [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ClearAllFavorites()
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
                return Unauthorized(new { error = "User not authenticated" });

            var result = await _favoriteService.ClearAllFavoritesAsync(userId.Value);
            return Ok(result);
        }

        //Get Favorite Stats (Admin Only)
        [HttpGet("stats")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(Response<FavoriteStatsVM>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetFavoriteStats()
        {
            var result = await _favoriteService.GetFavoriteStatsAsync();
            return Ok(result);
        }

        // Diagnostic endpoint to test authentication
        [HttpGet("debug/auth")]
        public IActionResult DebugAuth()
        {
            var userId = GetUserIdFromClaims();
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
             
            return Ok(new
            {
                  IsAuthenticated = User.Identity?.IsAuthenticated,
      UserId = userId,
             Claims = claims,
           IdentityName = User.Identity?.Name
            });
         }
    }
}
