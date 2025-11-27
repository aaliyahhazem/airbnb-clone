using BLL.ModelVM.Favorite;
using BLL.ModelVM.Response;
using Microsoft.AspNetCore.Mvc;

namespace PL.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FavoriteController : Controller
    {
        private readonly IFavoriteService _favoriteService;

        public FavoriteController(IFavoriteService favoriteService)
        {
            _favoriteService = favoriteService;
        }
        //Add New Favorite
        [HttpPost]
        [ProducesResponseType(typeof(Response<FavoriteVM>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> AddFavorite([FromBody] AddFavoriteVM model)
        {
            var userId = GetUserId();
            var result = await _favoriteService.AddFavoriteAsync(userId, model.ListingId);

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
            var userId = GetUserId();
            var result = await _favoriteService.RemoveFavoriteAsync(userId, listingId);

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
            var userId = GetUserId();
            var result = await _favoriteService.ToggleFavoriteAsync(userId, listingId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(new
            {
                isFavorited = result.result,
                message = result.result ? "Added to favorites" : "Removed from favorites"
            });
        }
        //Get My Favorites
        [HttpGet("me")]
        [ProducesResponseType(typeof(Response<List<FavoriteVM>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyFavorites()
        {
            var userId = GetUserId();
            var result = await _favoriteService.GetUserFavoritesAsync(userId);

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
            var userId = GetUserId();
            var result = await _favoriteService.GetUserFavoritesPaginatedAsync(userId, page, pageSize);

            return Ok(result);
        }
        //Check if Listing is Favorited
        [HttpGet("check/{listingId:int}")]
        [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CheckIsFavorited(int listingId)
        {
            var userId = GetUserId();
            var result = await _favoriteService.IsFavoritedAsync(userId, listingId);
            return Ok(result);
        }
        //Batch Check Favorites
        [HttpPost("check/batch")]
        [ProducesResponseType(typeof(Response<Dictionary<int, bool>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> BatchCheckFavorites([FromBody] BatchFavoriteCheckVM model)
        {
            var userId = GetUserId();
            var result = await _favoriteService.BatchCheckFavoritesAsync(userId, model.ListingIds);
            return Ok(result);
        }
        //Get My Favorites Count
        [HttpGet("me/count")]
        [ProducesResponseType(typeof(Response<int>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyFavoritesCount()
        {
            var userId = GetUserId();
            var result = await _favoriteService.GetUserFavoritesCountAsync(userId);
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
            var userId = GetUserId();
            var result = await _favoriteService.ClearAllFavoritesAsync(userId);

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
        private Guid GetUserId()
        {
            var possible = new[]
            {
                ClaimTypes.NameIdentifier,
                "sub",
                JwtRegisteredClaimNames.Sub,
                "id",
                "uid"
            };

            foreach (var name in possible)
            {
                var claim = User.FindFirst(name)?.Value;
                if (!string.IsNullOrEmpty(claim) && Guid.TryParse(claim, out var g))
                    return g;
            }

            var nameClaim = User.Identity?.Name;
            if (!string.IsNullOrEmpty(nameClaim) && Guid.TryParse(nameClaim, out var byName))
                return byName;

            throw new UnauthorizedAccessException("User ID not found in token");
        }
    }
}
