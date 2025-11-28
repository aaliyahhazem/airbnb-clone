namespace DAL.Repo.Implementation
{
    public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
    {
        public NotificationRepository(AppDbContext context) : base(context) { }

        public async Task<Notification> CreateAsync(Notification notification)
        {
            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();
            return notification;
        }

        public async Task<Notification> CreateAsync(Guid userId, string title, string body, DAL.Enum.NotificationType type)
        {
            var entity = Notification.Create(userId, title, body, type);
            await _context.Notifications.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync(Notification notification)
        {
            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public Task<IEnumerable<Notification>> GetPagedAsync(Guid userId, int page, int pageSize)
        {
            var result = _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsEnumerable();
            return Task.FromResult(result);
        }

        public async Task<IEnumerable<Notification>> GetPendingToSendAsync()
        {
            return await _context.Notifications
                .Where(n => !n.IsSentViaFCM)
                .OrderBy(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetUnreadAsync(Guid userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            return await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<bool> MarkAllAsReadAsync(Guid userId)
        {
            var unread = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unread)
                notification.MarkAsRead();

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAsReadAsync(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification is null) return false;

            notification.MarkAsRead();
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAsSentAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification is null) return false;
            notification.MarkAsSent();
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Notification> UpdateAsync(Notification notification)
        {
            _context.Notifications.Update(notification);
            await _context.SaveChangesAsync();
            return notification;
        }

       
    }

}
