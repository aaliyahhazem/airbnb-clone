using BLL.ModelVM.ListingVM;
using DAL.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace PL.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ListingsController : ControllerBase
    {
        private readonly IListingService _listingService;
        private readonly UserManager<User> _userManager;

        public ListingsController(IListingService listingService, UserManager<User> userManager)
        {
            _listingService = listingService;
            _userManager = userManager;
        }

        // 1) GET /api/listings?page=&pageSize= ==> homepage
        [HttpGet("Listings")]
        public async Task<IActionResult> GetAllListingsForHome(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default)
        {

            var result = await _listingService.GetPagedOverviewAsync(page, pageSize, null, ct);

            if (result.IsHaveErrorOrNo)
                return BadRequest(result);

            return Ok(result);
        }

        // 2) GET /api/listings/{id} => details
        [HttpGet("Listings/{id:int}")]
        public async Task<IActionResult> GetListingById([FromRoute] int id, CancellationToken ct = default)
        {

            var res = await _listingService.GetByIdWithImagesAsync(id, ct);

            if (res.IsHaveErrorOrNo)
                return BadRequest(res);

            return Ok(res);
        }

        // 3) POST /api/listings (Host Only)
        [HttpPost]
        //[Authorize(Roles = "Host")]
        public async Task<IActionResult> Create([FromForm] ListingCreateVM vm, CancellationToken ct = default)
        {

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var res = await _listingService.CreateAsync(vm, user.Id, ct);

            if (res.IsHaveErrorOrNo)
                return BadRequest(res);

            return CreatedAtAction(nameof(GetListingById), new { id = res.result }, res);
        }

        // 4) PUT /api/listings/{id} (Host Only)
        [HttpPut("Listings/{id:int}")]
        //[Authorize(Roles = "Host")]
        public async Task<IActionResult> Update(int id, [FromForm] ListingUpdateVM vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var res = await _listingService.UpdateAsync(id, user.Id, vm, ct);

            if (res.IsHaveErrorOrNo)
                return BadRequest(res);

            return Ok(res);
        }

        // 5) DELETE /api/listings/{id} (Host Only)
        // 
        [HttpDelete("Listings/{id:int}")]
        [Authorize(Roles = "Host")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var res = await _listingService.SoftDeleteByOwnerAsync(id, user.Id, ct);

            if (res.IsHaveErrorOrNo)
                return BadRequest(res);

            return Ok(res);
        }

        // 6) GET /api/hosts/{hostId}/listings
        [HttpGet("/api/hosts/{hostId:guid}/listings")]
        //[Authorize]
        public async Task<IActionResult> GetHostListings(
            Guid hostId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default)
        {
            var res = await _listingService.GetByUserAsync(hostId, page, pageSize, ct);

            if (res.IsHaveErrorOrNo)
                return BadRequest(res);

            return Ok(res);
        }

        // 7) PUT /api/admin/listings/{id}/approve
        [HttpPut("admin/Listings/{id:int}/approve")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id, CancellationToken ct = default)
        {
            var admin = await _userManager.GetUserAsync(User);
            if (admin == null)
                return Unauthorized();

            var res = await _listingService.ApproveAsync(id, admin.Id, ct);

            if (res.IsHaveErrorOrNo)
                return BadRequest(res);

            return Ok(res);
        }

        // 8) PUT /api/admin/listings/{id}/reject

        [HttpPut("admin/Listings/{id:int}/reject")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reject(int id, [FromBody] RejectListingRequest body, CancellationToken ct = default)
        {
            var admin = await _userManager.GetUserAsync(User);
            if (admin == null)
                return Unauthorized();

            var res = await _listingService.RejectAsync(id, admin.Id, body?.Note, ct);

            if (res.IsHaveErrorOrNo)
                return BadRequest(res);

            return Ok(res);
        }

        

        [HttpPut("{id:int}/promote")]
        //[Authorize]
        public async Task<IActionResult> Promote(int id, [FromBody] PromoteListingRequest body, CancellationToken ct = default)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var res = await _listingService.PromoteAsync(id, body.PromotionEndDate, user.Id, ct);

            if (res.IsHaveErrorOrNo)
                return BadRequest(res);

            return Ok(res);
        }

        // 10) PUT /api/listings/{listingId}/images/{imageId}/set-main
        [HttpPut("listings/{listingId:int}/images/{imageId:int}/set-main")]
        //[Authorize(Roles = "Host")]
        public async Task<IActionResult> SetMainImage(int listingId, int imageId, CancellationToken ct = default)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var res = await _listingService.SetMainImageAsync(listingId, imageId, user.Id, ct);

            if (res.IsHaveErrorOrNo)
                return BadRequest(res);

            return Ok(res);
        }


       

    }
}
