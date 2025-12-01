using BLL.ModelVM.ListingVM;
using DAL.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace PL.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ListingsController : BaseController
    {
        private readonly IListingService _listingService;

        public ListingsController(
            IListingService listingService)
        {
            _listingService = listingService;
            
        }

        // ---------------------------
        // PUBLIC LISTINGS API
        // ---------------------------
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll(
            [FromQuery] ListingFilterDto? filter,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default)
        {
            var result = await _listingService.GetPagedOverviewAsync(page, pageSize, filter, ct);
            if (result.IsHaveErrorOrNo) return BadRequest(result);

            // Return with totalCount for pagination (from database total, not current page count)
            var response = new
            {
                data = result.result,
                totalCount = result.TotalCount,
                message = result.errorMessage,
                isError = result.IsHaveErrorOrNo
            };

            return Ok(response);
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetListingById([FromRoute] int id, CancellationToken ct = default)
        {
            var result = await _listingService.GetByIdWithImagesAsync(id, ct);
            if (result.IsHaveErrorOrNo) return BadRequest(result);

            return Ok(result);
        }

        // ---------------------------
        // HOST OPERATIONS
        // ---------------------------

        [HttpPost]
        [Authorize(Roles = "Guest")]   // Hosts are users with role Guest or Host
        public async Task<IActionResult> Create([FromForm] ListingCreateVM vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserIdFromClaims();
            if (userId == null)
                return Unauthorized("Invalid or missing user ID in token.");

            var result = await _listingService.CreateAsync(vm, userId.Value, ct);
            if (result.IsHaveErrorOrNo) return BadRequest(result);

            return CreatedAtAction(nameof(GetListingById), new { id = result.result }, result);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> Update(int id, [FromForm] ListingUpdateVM vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();

            var result = await _listingService.UpdateAsync(id, userId.Value, vm, ct);
            if (result.IsHaveErrorOrNo) return BadRequest(result);

            return Ok(result);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();

            var result = await _listingService.SoftDeleteByOwnerAsync(id, userId.Value, ct);
            if (result.IsHaveErrorOrNo) return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("my-listings")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> GetHostListings(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();

            var result = await _listingService.GetByUserAsync(userId.Value, page, pageSize, ct);
            if (result.IsHaveErrorOrNo) return BadRequest(result);

            // Return with totalCount for pagination
            var response = new
            {
                data = result.result,
                totalCount = result.TotalCount,
                message = result.errorMessage,
                isError = result.IsHaveErrorOrNo
            };

            return Ok(response);
        }

        [HttpGet("has-listings")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> CheckUserHasListings(CancellationToken ct = default)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();

            var hasListings = await _listingService.UserHasListingsAsync(userId.Value, ct);

            return Ok(new
            {
                hasListings = hasListings,
                message = "Check user listings status",
                isError = false
            });
        }

        [HttpPut("{listingId:int}/image/{imageId:int}/main")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> SetMainImage(
            int listingId,
            int imageId,
            CancellationToken ct = default)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();

            var result = await _listingService.SetMainImageAsync(listingId, imageId, userId.Value, ct);
            if (result.IsHaveErrorOrNo) return BadRequest(result);

            return Ok(result);
        }

        // ---------------------------
        // ADMIN OPERATIONS
        // ---------------------------

        [HttpGet("admin/all-listings")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllListingsForAdmin(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default)
        {
            var adminId = GetUserIdFromClaims();
            if (adminId == null) return Unauthorized();

            var result = await _listingService.GetAllForAdminAsync(page, pageSize, ct);
            if (result.IsHaveErrorOrNo) return BadRequest(result);

            // Return with totalCount for pagination
            var response = new
            {
                data = result.result,
                totalCount = result.TotalCount,
                message = result.errorMessage,
                isError = result.IsHaveErrorOrNo
            };

            return Ok(response);
        }


        [HttpPut("admin/approve/listing/{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id, CancellationToken ct = default)
        {
            var adminId = GetUserIdFromClaims();
            if (adminId == null) return Unauthorized();

            var result = await _listingService.ApproveAsync(id, adminId.Value, ct);
            if (result.IsHaveErrorOrNo) return BadRequest(result);

            return Ok(result);
        }

        [HttpPut("admin/reject/listing/{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reject(
            int id,
            [FromBody] RejectListingRequest? body,
            CancellationToken ct = default)
        {
            var adminId = GetUserIdFromClaims();
            if (adminId == null) return Unauthorized();

            var result = await _listingService.RejectAsync(id, adminId.Value, body?.Note, ct);
            if (result.IsHaveErrorOrNo) return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("admin/promote/{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PromoteListing(
            int id,
            [FromBody] PromoteListingRequest request,
            CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var adminId = GetUserIdFromClaims();
            if (adminId == null) return Unauthorized();

            var result = await _listingService.PromoteAsync(id, request.PromotionEndDate, adminId.Value, ct);
            if (result.IsHaveErrorOrNo) return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("admin/unpromote/{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UnpromoteListing(int id, CancellationToken ct = default)
        {
            var adminId = GetUserIdFromClaims();
            if (adminId == null) return Unauthorized();

            var result = await _listingService.UnpromoteAsync(id, adminId.Value, ct);
            if (result.IsHaveErrorOrNo) return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("admin/extend-promotion/{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExtendPromotion(
            int id,
            [FromBody] PromoteListingRequest request,
            CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var adminId = GetUserIdFromClaims();
            if (adminId == null) return Unauthorized();

            var result = await _listingService.ExtendPromotionAsync(id, request.PromotionEndDate, adminId.Value, ct);
            if (result.IsHaveErrorOrNo) return BadRequest(result);

            return Ok(result);
        }

    }
}
