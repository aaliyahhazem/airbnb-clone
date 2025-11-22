namespace DAL.Repo.Abstraction
{
    public interface IMessageRepository : IGenericRepository<Message>
    {
        Task<IEnumerable<Message>> GetConversationAsync(Guid userId1, Guid userId2);     // Chat between 2 users
        Task<IEnumerable<Message>> GetUnreadMessagesAsync(Guid receiverId);              // Unread messages for a receiver
        // Repository handles entity creation
        Task<Message> CreateAsync(Guid senderId, Guid receiverId, string content, DateTime sentAt, bool isRead);
    }
}