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

        public async Task<int> GetUnreadCountAsync(Guid receiverId)
        {
            return await _context.Messages.CountAsync(m => m.ReceiverId == receiverId && !m.IsRead);
        }

        public async Task<IEnumerable<DAL.Dto.ConversationDto>> GetConversationsAsync(Guid userId)
        {
            // Group messages by the other participant
            var query = _context.Messages
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .Select(m => new
                {
                    Message = m,
                    OtherId = m.SenderId == userId ? m.ReceiverId : m.SenderId
                });

            var grouped = await query
                .GroupBy(x => x.OtherId)
                .Select(g => new DAL.Dto.ConversationDto
                {
                    OtherUserId = g.Key,
                    LastMessage = g.OrderByDescending(x => x.Message.SentAt).Select(x => x.Message.Content).FirstOrDefault(),
                    LastSentAt = g.OrderByDescending(x => x.Message.SentAt).Select(x => (DateTime?)x.Message.SentAt).FirstOrDefault(),
                    UnreadCount = g.Count(x => x.Message.ReceiverId == userId && !x.Message.IsRead)
                })
                .OrderByDescending(c => c.LastSentAt)
                .ToListAsync();

            // populate OtherUserName by joining users
            var userIds = grouped.Select(g => g.OtherUserId).ToList();
            var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
            foreach (var conv in grouped)
            {
                var u = users.FirstOrDefault(x => x.Id == conv.OtherUserId);
                conv.OtherUserName = u?.UserName ?? string.Empty;
            }

            return grouped;
        }

        public async Task<Message?> MarkAsReadAsync(int messageId)
        {
            var msg = await _context.Messages.FindAsync(messageId);
            if (msg == null) return null;
            if (!msg.IsRead)
            {
                msg.IsRead = true;
                await _context.SaveChangesAsync();
            }
            return msg;
        }

        public async Task<int> MarkAllAsReadAsync(Guid receiverId)
        {
            var unread = await _context.Messages.Where(m => m.ReceiverId == receiverId && !m.IsRead).ToListAsync();
            if (!unread.Any()) return 0;
            foreach (var m in unread) m.IsRead = true;
            await _context.SaveChangesAsync();
            return unread.Count;
        }

        public async Task<bool> DeleteAsync(int messageId)
        {
            var msg = await _context.Messages.FindAsync(messageId);
            if (msg == null) return false;
            _context.Messages.Remove(msg);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
