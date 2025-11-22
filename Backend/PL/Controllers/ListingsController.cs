using BLL.ModelVM.ListingVM;
using BLL.Services.Abstractions;
using DAL.Entities;
using Microsoft.AspNetCore.Authorization;
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

        public ListingsController(
            IListingService listingService,
            UserManager<User> userManager)
        {
            _listingService = listingService;
            _userManager = userManager;
        }

        // Public list
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] ListingFilterDto? filter,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default)
        {
            var result = await _listingService.GetPagedOverviewAsync(page, pageSize, filter, ct);
            if (result.IsHaveErrorOrNo) return BadRequest(result);
            return Ok(result);
        }

        // Public details
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetListingById([FromRoute] int id, CancellationToken ct = default)
        {
            var res = await _listingService.GetByIdWithImagesAsync(id, ct);
            if (res.IsHaveErrorOrNo) return BadRequest(res);
            return Ok(res);
        }

        // Host create listing (with images in vm.Images)
        [HttpPost]
        //[Authorize(Roles = "Host")]
        public async Task<IActionResult> Create([FromForm] ListingCreateVM vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var res = await _listingService.CreateAsync(vm, user.Id, ct);
            if (res.IsHaveErrorOrNo) return BadRequest(res);

            return CreatedAtAction(nameof(GetListingById), new { id = res.result }, res);
        }

        // Host update listing (add/remove images via vm.NewImages & vm.RemoveImageIds)
        [HttpPut("{id:int}")]
        //[Authorize(Roles = "Host")]
        public async Task<IActionResult> Update(int id, [FromForm] ListingUpdateVM vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var res = await _listingService.UpdateAsync(id, user.Id, vm, ct);
            if (res.IsHaveErrorOrNo) return BadRequest(res);

            return Ok(res);
        }

        // Host soft delete listing
        [HttpDelete("{id:int}")]
        //[Authorize(Roles = "Host")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var res = await _listingService.SoftDeleteByOwnerAsync(id, user.Id, ct);
            if (res.IsHaveErrorOrNo) return BadRequest(res);

            return Ok(res);
        }

        // Host's own listings
        [HttpGet("AllhostListings")]
        //[Authorize(Roles = "Host")]
        public async Task<IActionResult> GetHostListings(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var res = await _listingService.GetByUserAsync(user.Id, page, pageSize, ct);
            if (res.IsHaveErrorOrNo) return BadRequest(res);

            return Ok(res);
        }

        // Admin approve listing
        [HttpPut("admin/approve/listing/{id:int}")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id, CancellationToken ct = default)
        {
            var admin = await _userManager.GetUserAsync(User);
            if (admin == null) return Unauthorized();

            var res = await _listingService.ApproveAsync(id, admin.Id, ct);
            if (res.IsHaveErrorOrNo) return BadRequest(res);

            return Ok(res);
        }

        // Admin reject listing
        [HttpPut("admin/reject/listing/{id:int}")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reject(int id, [FromBody] RejectListingRequest? body, CancellationToken ct = default)
        {
            var admin = await _userManager.GetUserAsync(User);
            if (admin == null) return Unauthorized();

            var res = await _listingService.RejectAsync(id, admin.Id, body?.Note, ct);
            if (res.IsHaveErrorOrNo) return BadRequest(res);

            return Ok(res);
        }

     

        // Set main image via ListingService (still "listing-centric")
        [HttpPut("{listingId:int}/image/{imageId:int}/main")]
        [Authorize(Roles = "Host")]
        public async Task<IActionResult> SetMainImage(
            int listingId,
            int imageId,
            CancellationToken ct = default)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var res = await _listingService.SetMainImageAsync(listingId, imageId, user.Id, ct);
            if (res.IsHaveErrorOrNo) return BadRequest(res);

            return Ok(res);
        }
    }
}
