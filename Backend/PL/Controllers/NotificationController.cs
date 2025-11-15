using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace PL.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
   // [Authorize]   // Require JWT
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // Helper to read user id from token
        private Guid GetUserId()
        {
            //return Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            return Guid.Parse("5ec2b472-1e94-4370-d58d-08de23d16cc5");
        }

        // --------------------------- CREATE -------------------------------
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateNotificationVM model)
        {
            model.UserId = GetUserId();
            var result = await _notificationService.CreateAsync(model);

            if (result.IsHaveErrorOrNo )
                return BadRequest(result);

            return Ok(result);
        }

        // --------------------------- GET BY ID ----------------------------
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _notificationService.GetByIdAsync(id);

            if (result.IsHaveErrorOrNo)
                return NotFound(result);

            return Ok(result);
        }

        // --------------------------- GET USER NOTIFICATIONS ---------------
        [HttpGet("user")]
        public async Task<IActionResult> GetForCurrentUser()
        {
            var userId = GetUserId();
            var result = await _notificationService.GetByUserIdAsync(userId);

            return Ok(result);
        }

        // --------------------------- PAGED --------------------------------
        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var userId = GetUserId();
            var result = await _notificationService.GetPagedAsync(userId, page, pageSize);

            return Ok(result);
        }

        // --------------------------- UNREAD ONLY ---------------------------
        [HttpGet("unread")]
        public async Task<IActionResult> GetUnread()
        {
            var userId = GetUserId();
            var result = await _notificationService.GetUnreadAsync(userId);

            return Ok(result);
        }

        // --------------------------- MARK ONE AS READ ----------------------
        [HttpPut("{id:int}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var result = await _notificationService.MarkAsReadAsync(id);

            if (result.IsHaveErrorOrNo)
                return BadRequest(result);

            return Ok(result);
        }

        // --------------------------- MARK ALL AS READ ----------------------
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = GetUserId();
            var result = await _notificationService.MarkAllAsReadAsync(userId);

            if (result.IsHaveErrorOrNo)
                return BadRequest(result);

            return Ok(result);
        }

        // --------------------------- SEND PENDING (Cron / Background Job) --
        [HttpPost("send-pending")]
        [AllowAnonymous] 
        public async Task<IActionResult> SendPending()
        {
            var result = await _notificationService.SendPendingNotificationsAsync();

            return Ok(result);
        }
    }
}
