namespace PL.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewController : BaseController
    {
        private readonly IReviewService _reviewService;
        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpPost]
        [Authorize(Roles = "Guest,Admin")] // guest creates review
        public async Task<IActionResult> Create([FromBody] CreateReviewVM model)
        {
            var uid = GetUserIdFromClaims();
            if (uid == null) return Unauthorized(BLL.ModelVM.Response.Response<ReviewVM>.FailResponse("Invalid or missing user identifier."));

            var res = await _reviewService.CreateReviewAsync(model, uid.Value);
            if (res.IsHaveErrorOrNo) return BadRequest(res);
            return Ok(res);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Guest,Admin")] // guest or admin can edit
        public async Task<IActionResult> Update(int id, [FromBody] UpdateReviewVM model)
        {
            var res = await _reviewService.UpdateReviewAsync(id, model);
            if (res.IsHaveErrorOrNo) return BadRequest(res);
            return Ok(res);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")] // admin can delete
        public async Task<IActionResult> Delete(int id)
        {
            var res = await _reviewService.DeleteReviewAsync(id);
            if (res.IsHaveErrorOrNo) return BadRequest(res);
            return Ok(res);
        }

        [HttpGet("listing/{listingId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByListing(int listingId)
        {
            var res = await _reviewService.GetReviewsByListingAsync(listingId);
            return Ok(res);
        }

        [HttpGet("guest/{guestId:guid}")]
        [Authorize(Roles = "Guest,Host,Admin")]
        public async Task<IActionResult> GetByGuest(Guid guestId)
        {
            var res = await _reviewService.GetReviewsByGuestAsync(guestId);
            return Ok(res);
        }

        [HttpGet("listing/{listingId:int}/avg")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAvg(int listingId)
        {
            var res = await _reviewService.GetAverageRatingAsync(listingId);
            return Ok(res);
        }
    }
}
