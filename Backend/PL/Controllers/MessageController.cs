using BLL.ModelVM.Message;
using BLL.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PL.Hubs;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace PL.Controllers
{
 [Route("api/[controller]")]
 [ApiController]
 [Authorize]
 public class MessageController : ControllerBase
 {
 private readonly IMessageService _messageService;
 private readonly IHubContext<MessageHub> _hub;

 public MessageController(IMessageService messageService, IHubContext<MessageHub> hub)
 {
 _messageService = messageService;
 _hub = hub;
 }

 private Guid GetUserId()
 {
 var sub = User.FindFirst("sub")?.Value;
 if (!string.IsNullOrWhiteSpace(sub) && Guid.TryParse(sub, out var uid)) return uid;
 // fallback for dev
 return Guid.Parse("729a642d-9885-40b2-2817-08de255a2d0a");
 }

 [HttpGet("conversation/{otherUserId:guid}")]
 public async Task<IActionResult> GetConversation(Guid otherUserId)
 {
 var myId = GetUserId();
 var result = await _messageService.GetConversationAsync(myId, otherUserId);
 return Ok(result);
 }

 [HttpGet("unread")]
 public async Task<IActionResult> GetUnread()
 {
 var myId = GetUserId();
 var result = await _messageService.GetUnreadAsync(myId);
 return Ok(result);
 }

 [HttpPost]
 public async Task<IActionResult> Create([FromBody] CreateMessageVM model)
 {
 model.SenderId = GetUserId();
 var result = await _messageService.CreateAsync(model);
 if (result.IsHaveErrorOrNo)
 return BadRequest(result);

 // load created entity to get fields like Id / SentAt
 var conv = await _messageService.GetConversationAsync(model.SenderId, model.ReceiverId);
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
