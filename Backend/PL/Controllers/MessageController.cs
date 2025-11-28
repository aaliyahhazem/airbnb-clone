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

        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var myId = GetUserIdFromClaims();
            if (myId == null) return Unauthorized();
            var result = await _messageService.GetConversationsAsync(myId.Value);
            return Ok(result);
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var myId = GetUserIdFromClaims();
            if (myId == null) return Unauthorized();

            var result = await _messageService.GetUnreadCountAsync(myId.Value);
            if (result.IsHaveErrorOrNo) return BadRequest(result);
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
            // call service with sender id param; service will resolve receiver username
            var result = await _messageService.CreateAsync(model, myId.Value);
            if (result.IsHaveErrorOrNo)
                return BadRequest(result);

            // load created entity to get fields like Id / SentAt
            // need to find the receiver id by username
            var receiver = await _messageService.GetUserByUserNameAsync(model.ReceiverUserName);
            var receiverId = receiver?.Id;
            var conv = receiverId.HasValue ? await _messageService.GetConversationAsync(myId.Value, receiverId.Value) : null;
            var last = conv?.result?.OrderByDescending(m => m.SentAt).FirstOrDefault();

            var connectionId = receiverId.HasValue ? MessageHub.GetConnectionId(receiverId.Value.ToString()) : null;
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

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var myId = GetUserIdFromClaims();
            if (myId == null) return Unauthorized();

            var result = await _messageService.MarkAsReadAsync(id, myId.Value);
            if (result.IsHaveErrorOrNo) return BadRequest(result);

            // notify sender that this message was read
            var updated = result.result;
            if (updated != null)
            {
                var connectionId = MessageHub.GetConnectionId(updated.SenderId.ToString());
                if (!string.IsNullOrEmpty(connectionId))
                {
                    await _hub.Clients.Client(connectionId)
                      .SendAsync("MessageRead", new { messageId = updated.Id, readerId = myId.Value });
                }
            }

            return Ok(result);
        }

        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            var myId = GetUserIdFromClaims();
            if (myId == null) return Unauthorized();
            var result = await _messageService.MarkAllAsReadAsync(myId.Value);
            if (result.IsHaveErrorOrNo) return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var myId = GetUserIdFromClaims();
            if (myId == null) return Unauthorized();
            var result = await _messageService.DeleteMessageAsync(id, myId.Value);
            if (result.IsHaveErrorOrNo) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("userbyname/{username}")]
        public async Task<IActionResult> GetUserByName(string username)
        {
            var user = await _messageService.GetUserByUserNameAsync(username);
            if (user == null) return NotFound();
            return Ok(new { id = user.Id, userName = user.UserName });
        }
    }
}
