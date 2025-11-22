namespace PL.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationController : BaseController
    {
        private readonly INotificationService _notificationService;
        private readonly IHubContext<NotificationHub> _hub;


        public NotificationController(INotificationService notificationService, IHubContext<NotificationHub> hub)
        {
            _notificationService = notificationService;
            _hub = hub;
        }

        //--------------------------- CREATE -------------------------------
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateNotificationVM model)
        {
            var uid = GetUserIdFromClaims();
            if (uid == null) return Unauthorized();
            model.UserId = uid.Value; // set server-side
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

        // --------------------------- GET ALL ------------------------------
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _notificationService.GetAllAsync();
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
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();
            var result = await _notificationService.GetByUserIdAsync(userId.Value);

            return Ok(result);
        }

        // --------------------------- PAGED --------------------------------
        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged([FromQuery] int page =1, [FromQuery] int pageSize =10)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();
            var result = await _notificationService.GetPagedAsync(userId.Value, page, pageSize);

            return Ok(result);
        }

        // --------------------------- UNREAD ONLY ---------------------------
        [HttpGet("unread")]
        public async Task<IActionResult> GetUnread()
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();
            var result = await _notificationService.GetUnreadAsync(userId.Value);

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
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();
            var result = await _notificationService.MarkAllAsReadAsync(userId.Value);

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
