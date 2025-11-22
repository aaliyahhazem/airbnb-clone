namespace PL.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingController : BaseController
    {
        private readonly IBookingService _bookingService;

        public BookingController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBookingVM model)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();

            var resp = await _bookingService.CreateBookingAsync(userId.Value, model);
            if (!resp.Success) return BadRequest(resp.errorMessage);
            return Ok(resp.result);
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();

            var resp = await _bookingService.CancelBookingAsync(userId.Value, id);
            if (!resp.Success) return BadRequest(resp.errorMessage);
            return Ok(resp.result);
        }

        [HttpGet("me")]
        public async Task<IActionResult> MyBookings()
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();

            var resp = await _bookingService.GetBookingsByUserAsync(userId.Value);
            if (!resp.Success) return BadRequest(resp.errorMessage);
            return Ok(resp.result);
        }

        [HttpGet("host/me")]
        public async Task<IActionResult> HostBookings()
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();

            var resp = await _bookingService.GetBookingsByHostAsync(userId.Value);
            if (!resp.Success) return BadRequest(resp.errorMessage);
            return Ok(resp.result);
        }
    }
}
