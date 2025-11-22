namespace BLL.Services.Abstractions
{
 public interface IMessageService
 {
 Task<Response<List<GetMessageVM>>> GetConversationAsync(Guid userId1, Guid userId2);
 Task<Response<List<GetMessageVM>>> GetUnreadAsync(Guid receiverId);
 Task<Response<CreateMessageVM>> CreateAsync(CreateMessageVM model, Guid senderId);
 }
}
