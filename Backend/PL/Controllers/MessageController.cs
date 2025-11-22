namespace PL.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MessageController : BaseController
    {
        private readonly IMessageService _messageService;
        private readonly IHubContext<MessageHub> _hub;

        public MessageController(IMessageService messageService, IHubContext<MessageHub> hub)
        {
            _messageService = messageService;
            _hub = hub;
        }

        [HttpGet("conversation/{otherUserId:guid}")]
        public async Task<IActionResult> GetConversation(Guid otherUserId)
        {
            var myId = GetUserIdFromClaims();
            if (myId == null) return Unauthorized();
            var result = await _messageService.GetConversationAsync(myId.Value, otherUserId);
            return Ok(result);
        }

        [HttpGet("unread")]
        public async Task<IActionResult> GetUnread()
        {
            var myId = GetUserIdFromClaims();
            if (myId == null) return Unauthorized();
            var result = await _messageService.GetUnreadAsync(myId.Value);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateMessageVM model)
        {
            var myId = GetUserIdFromClaims();
            if (myId == null) return Unauthorized();
            var newModel = new CreateMessageVM { ReceiverId = model.ReceiverId, Content = model.Content };
            // call service with sender id param
            var result = await _messageService.CreateAsync(newModel, myId.Value);
            if (result.IsHaveErrorOrNo)
                return BadRequest(result);

            // load created entity to get fields like Id / SentAt
            var conv = await _messageService.GetConversationAsync(myId.Value, model.ReceiverId);
            var last = conv.result?.OrderByDescending(m => m.SentAt).FirstOrDefault();

            var connectionId = MessageHub.GetConnectionId(model.ReceiverId.ToString());
            if (!string.IsNullOrEmpty(connectionId) && last != null)
            {
                await _hub.Clients.Client(connectionId)
                .SendAsync("ReceiveMessage", new
                {
                    Id = last.Id,
                    SenderId = last.SenderId,
                    ReceiverId = last.ReceiverId,
                    Content = last.Content,
                    SentAt = last.SentAt,
                    IsRead = last.IsRead
                });
            }

            return Ok(result);
        }
    }
}
