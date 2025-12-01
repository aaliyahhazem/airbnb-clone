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
            // Dev-friendly debug logging: record incoming booking payload and resolved user id
            try
            {
                // Use Information so it appears in default dev logs and is easier to spot
                _logger?.LogInformation("Booking Create request received. userId={UserId}, listingId={ListingId}, checkIn={CheckIn}, checkOut={CheckOut}, guests={Guests}, paymentMethod={PaymentMethod}",
                    userId?.ToString() ?? "<null>", model?.ListingId, model?.CheckInDate.ToString("o"), model?.CheckOutDate.ToString("o"), model?.Guests, model?.PaymentMethod);
            }
            catch { /* don't let logging break the flow */ }
            if (userId == null) return Unauthorized();

            var resp = await _bookingService.CreateBookingAsync(userId.Value, model);
            if (!resp.Success)
            {
                // Log failure reason for easier debugging during development
                try { _logger?.LogWarning("Booking create failed for user {UserId}: {Reason}", userId?.ToString() ?? "<null>", resp.errorMessage); } catch {}
                return BadRequest(resp.errorMessage);
            }
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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();

            var resp = await _bookingService.GetByIdAsync(userId.Value, id);
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
