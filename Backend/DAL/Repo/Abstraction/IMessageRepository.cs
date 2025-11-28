namespace DAL.Repo.Abstraction
{
    public interface IMessageRepository : IGenericRepository<Message>
    {
        Task<IEnumerable<Message>> GetConversationAsync(Guid userId1, Guid userId2);     // Chat between 2 users
        Task<IEnumerable<Message>> GetUnreadMessagesAsync(Guid receiverId);              // Unread messages for a receiver
        Task<int> GetUnreadCountAsync(Guid receiverId);                                  // Unread messages count for a receiver
        Task<IEnumerable<DAL.Dto.ConversationDto>> GetConversationsAsync(Guid userId);    // Conversations summary for a user
        // Repository handles entity creation
        Task<Message> CreateAsync(Guid senderId, Guid receiverId, string content, DateTime sentAt, bool isRead);
        Task<Message?> MarkAsReadAsync(int messageId);
        Task<int> MarkAllAsReadAsync(Guid receiverId);                                     // returns count updated
        Task<bool> DeleteAsync(int messageId);
    }
}