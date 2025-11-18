using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PL.Hubs;
using System.Runtime.InteropServices;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace PL.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly IHubContext<NotificationHub> _hub;


        public NotificationController(INotificationService notificationService, IHubContext<NotificationHub> hub)
        {
            _notificationService = notificationService;
            _hub = hub;
        }

        // Helper to read user id from token
        private Guid GetUserId()
        {
            var sub = User.FindFirst("sub")?.Value;
            if (!string.IsNullOrWhiteSpace(sub) && Guid.TryParse(sub, out var uid)) return uid;
            // fallback for dev
            return Guid.Parse("70188af8-4575-49d8-5822-08de25168bf9");
        }
        //--------------------------- GET ALL ------------------------------
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _notificationService.GetAllAsync();
            return Ok(result);
        }
        // --------------------------- CREATE -------------------------------
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateNotificationVM model)
        {
            model.UserId = GetUserId();
            var result = await _notificationService.CreateAsync(model);

            if (result.IsHaveErrorOrNo )
                return BadRequest(result);

            //send the real time notification by signalR
            var connectionId = NotificationHub.GetConnectionId(result.result.UserId.ToString());
            if (!string.IsNullOrEmpty(connectionId))
            {
                await _hub.Clients.Client(connectionId)
                          .SendAsync("ReceiveNotification", new
                          {
                              Id = result.result.Id,
                              UserId = result.result.UserId,
                              Title = result.result.Title,
                              Body = result.result.Body,
                              CreatedAt = result.result.CreatedAt,
                              IsRead = result.result.IsRead,
                          });
            }

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
