namespace BLL.Services.Abstractions
{
 public interface IMessageService
 {
 Task<Response<List<GetMessageVM>>> GetConversationAsync(Guid userId1, Guid userId2);
 Task<Response<List<GetMessageVM>>> GetUnreadAsync(Guid receiverId);
 Task<Response<int>> GetUnreadCountAsync(Guid receiverId);
 Task<Response<CreateMessageVM>> CreateAsync(CreateMessageVM model, Guid senderId);
 Task<Response<List<BLL.ModelVM.Message.ConversationVM>>> GetConversationsAsync(Guid userId);
 Task<DAL.Entities.User?> GetUserByUserNameAsync(string userName);
 Task<Response<GetMessageVM>> MarkAsReadAsync(int messageId, Guid userId);
 Task<Response<int>> MarkAllAsReadAsync(Guid receiverId);
 Task<Response<bool>> DeleteMessageAsync(int messageId, Guid userId);
 }
}
