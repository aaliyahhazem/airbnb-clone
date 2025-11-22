namespace DAL.Repo.Implementation
{
    public class MessageRepository : GenericRepository<Message>, IMessageRepository
    {
        public MessageRepository(AppDbContext context) : base(context) { }

        public async Task<Message> CreateAsync(Guid senderId, Guid receiverId, string content, DateTime sentAt, bool isRead)
        {
            var entity = Message.Create(senderId, receiverId, content, sentAt, isRead);
            await _context.Messages.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<IEnumerable<Message>> GetConversationAsync(Guid userId1, Guid userId2)
        {
            return await _context.Messages
                .Where(m =>
                    (m.SenderId == userId1 && m.ReceiverId == userId2) ||
                    (m.SenderId == userId2 && m.ReceiverId == userId1))
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Message>> GetUnreadMessagesAsync(Guid receiverId)
        {
            return await _context.Messages
                .Where(m => m.ReceiverId == receiverId && !m.IsRead)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();
        }
    }
}
