namespace PL.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingController : BaseController
    {
        private readonly IBookingService _bookingService;
        private readonly ILogger<BookingController> _logger;
        private readonly IEmailService _emailService;
        private readonly EmailMappingService _emailMappingService;
        private readonly IUnitOfWork _uow;

        public BookingController(
            IBookingService bookingService,
            ILogger<BookingController> logger,
            IEmailService emailService,
            EmailMappingService emailMappingService,
            IUnitOfWork uow)
        {
            _bookingService = bookingService;
            _logger = logger;
            _emailService = emailService;
            _emailMappingService = emailMappingService;
            _uow = uow;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBookingVM model)
        {
            var userId = GetUserIdFromClaims();

            if (userId == null) return Unauthorized();

            // Log incoming request
            try
            {
                _logger?.LogInformation(
                    "Booking Create request received. userId={UserId}, listingId={ListingId}",
                    userId?.ToString() ?? "<null>", model?.ListingId);
                _logger?.LogInformation("Booking Create request received. userId={UserId}, listingId={ListingId}, checkIn={CheckIn}, checkOut={CheckOut}, guests={Guests}, paymentMethod={PaymentMethod}",
                    userId?.ToString() ?? "<null>", model?.ListingId, model?.CheckInDate.ToString("o"), model?.CheckOutDate.ToString("o"), model?.Guests, model?.PaymentMethod);
            }
            catch { }

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
            var resp = await _bookingService.CreateBookingAsync(userId.Value, model);
            if (!resp.Success)
            {
                try { _logger?.LogWarning("Booking create failed for user {UserId}: {Reason}", userId?.ToString() ?? "<null>", resp.errorMessage); } catch { }
                return BadRequest(resp.errorMessage);
            }
        }

            var bookingVM = resp.result;

            // --- Load full booking with related entities (Listing, Guest, Host, Payment) ---
            var fullBooking = await _uow.Bookings.GetByIdWithListingAndHostAsync(bookingVM.Id);
            if (fullBooking == null)
            {
                _logger.LogError("Full booking entity not found for bookingId={BookingId}", bookingVM.Id);
                return Ok(bookingVM); // still return success
            }

            // --- Guest booking confirmation email ---
            try
            {
                var confirmVM = _emailMappingService.ToBookingConfirmationVM(fullBooking);
                await _emailService.SendBookingConfirmationAsync(confirmVM);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed sending guest booking confirmation for bookingId={BookingId}", fullBooking.Id);
            }

            // --- Host notification email ---
            try
            {
                var hostVM = _emailMappingService.ToHostNewBookingVM(fullBooking);
                if (!string.IsNullOrWhiteSpace(hostVM.HostEmail) && hostVM.HostEmail != fullBooking.Guest.Email)
                    await _emailService.SendHostNewBookingAsync(hostVM);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed sending host new booking email for bookingId={BookingId}", fullBooking.Id);
            }

            // --- Payment receipt email ---
            try
            {
                if (fullBooking.Payment != null)
                {
                    var paymentVM = _emailMappingService.ToPaymentReceiptVM(fullBooking);
                    await _emailService.SendPaymentReceiptAsync(paymentVM);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed sending payment receipt for bookingId={BookingId}", fullBooking.Id);
            }

            // --- Payout notification to host ---
            try
            {
                if (fullBooking.Payment != null && fullBooking.GuestId != fullBooking.Listing.UserId)
                {
                    var payoutVM = _emailMappingService.ToPayoutNotificationVM(fullBooking);
                    await _emailService.SendPayoutNotificationAsync(payoutVM);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed sending payout notification for bookingId={BookingId}", fullBooking.Id);
            }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)

            return Ok(bookingVM);
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id)
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
            var resp = await _bookingService.CancelBookingAsync(userId.Value, id);
            if (!resp.Success) return BadRequest(resp.errorMessage);

            try
            {
                var fullBooking = await _uow.Bookings.GetByIdWithListingAndHostAsync(id);
                if (fullBooking != null)
                {
                    var cancelVM = _emailMappingService.ToCancellationVM(fullBooking, cancelledByHost: false);
                    await _emailService.SendCancellationEmailAsync(cancelVM);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed sending cancellation email");
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
