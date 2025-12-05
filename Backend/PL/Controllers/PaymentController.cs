namespace PL.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : BaseController
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }
        private Guid? GetUserIdFromClaims()
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

            return null;
        }   
        #region Stripe Endpoints 
        [HttpPost("stripe/create-intent")]
        [Authorize]
        public async Task<IActionResult> CreateStripePaymentIntent([FromBody] CreateStripePaymentVM model)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();
            var resp = await _paymentService.CreateStripePaymentIntentAsync(userId.Value, model);
            if (!resp.Success)
            {
                return BadRequest(new { error = resp.errorMessage });
            }
            return Ok(resp.result);
        }

        // Stripe webhook endpoint - handles payment events from Stripe
        [HttpPost("stripe/webhook")]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
            public async Task<IActionResult> StripeWebhook()
            {
                var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                var signature = Request.Headers["Stripe-Signature"].ToString();

                try
                {
                    var result = await _paymentService.HandleStripeWebhookAsync(json, signature);
                    if (result == null || !result.Success)
                    {
                        return BadRequest(new { error = result?.errorMessage ?? "Webhook processing failed" });
                    }
                    return Ok();
                }
                catch (StripeException ex)
                {
                    return BadRequest(new { error = "Invalid stripe signature" });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { error = "Internal webhook error" });
                }
            }
        // Cancel a Stripe payment intent
        [HttpPost("stripe/cancel/{paymentIntentId}")]
        [Authorize]
        public async Task<IActionResult> CancelStripePayment(string paymentIntentId)
        {
            var resp = await _paymentService.CancelStripePaymentAsync(paymentIntentId);
            if (!resp.Success) return BadRequest(new { error = resp.errorMessage });

            return Ok(new { success = true });
        }
        #endregion

        [HttpPost("initiate")]
        public async Task<IActionResult> Initiate([FromBody] CreatePaymentVM model)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();

            var resp = await _paymentService.InitiatePaymentAsync(userId.Value, model.BookingId, model.Amount, model.PaymentMethod);
            if (!resp.Success) return BadRequest(resp.errorMessage);
            return Ok(resp.result);
        }

        [HttpPost("confirm")]
        public async Task<IActionResult> Confirm([FromBody] dynamic body)
        {
            int bookingId = (int)body.bookingId;
            string tx = (string)body.transactionId;
            var resp = await _paymentService.ConfirmPaymentAsync(bookingId, tx);
            if (!resp.Success) return BadRequest(resp.errorMessage);
            return Ok(resp.result);
        }

        [HttpPost("{id}/refund")]
        public async Task<IActionResult> Refund(int id)
        {
            var resp = await _paymentService.RefundPaymentAsync(id);
            if (!resp.Success) return BadRequest(resp.errorMessage);
            return Ok(resp.result);
        }
    }
}
