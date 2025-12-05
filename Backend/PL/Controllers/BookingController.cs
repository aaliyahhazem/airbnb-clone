namespace PL.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingController : BaseController
    {
        private readonly IBookingService _bookingService;
        private readonly ILogger<BookingController> _logger;

        public BookingController(IBookingService bookingService, ILogger<BookingController> logger)
        {
            _bookingService = bookingService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBookingVM model)
        {
            var userId = GetUserIdFromClaims();

            try
            {
                _logger?.LogInformation(
                    "Booking Create request received. userId={UserId}, listingId={ListingId}",
                    userId?.ToString() ?? "<null>", model?.ListingId);

                if (userId == null)
                    return Unauthorized(new { success = false, errorMessage = "Unauthorized" });

                var resp = await _bookingService.CreateBookingAsync(userId.Value, model);

                if (!resp.Success)
                {
                    _logger?.LogWarning("Booking create failed: {Reason}", resp.errorMessage);
                    return BadRequest(new
                    {
                        success = false,
                        errorMessage = resp.errorMessage
                    });
                }

                // ? Return consistent wrapped response
                return Ok(new
                {
                    success = true,
                    result = resp.result,
                    errorMessage = (string?)null
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "CreateBooking exception");
                return StatusCode(500, new
                {
                    success = false,
                    errorMessage = ex.Message
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
                return Unauthorized(new { success = false, errorMessage = "Unauthorized" });

            var resp = await _bookingService.GetByIdAsync(userId.Value, id);

            if (!resp.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    errorMessage = resp.errorMessage
                });
            }

            // ? Return consistent wrapped response
            return Ok(new
            {
                success = true,
                result = resp.result,
                errorMessage = (string?)null
            });
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
