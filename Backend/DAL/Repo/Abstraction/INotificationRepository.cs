namespace DAL.Repo.Abstraction
{
    public interface INotificationRepository : IGenericRepository<Notification>
    {

        Task<Notification?> GetByIdAsync(int id);
        Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<Notification>> GetUnreadAsync(Guid userId);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task<IEnumerable<Notification>> GetPagedAsync(Guid userId, int page, int pageSize);
        Task<IEnumerable<Notification>> GetPendingToSendAsync();

        Task<Notification> CreateAsync(Notification notification);
        Task<Notification> CreateAsync(Guid userId, string title, string body, DAL.Enum.NotificationType type); // convenience overload - repository creates and persists notification entity
        Task<Notification> CreateAsync(Guid userId, string title, string body, DAL.Enum.NotificationType type, string? actionUrl, string? actionLabel); // convenience overload with action button
        Task<Notification> UpdateAsync(Notification notification);
        Task <bool> DeleteAsync(Notification notification);

        Task<Notification?> MarkAsReadAsync(int notificationId);
        Task<bool> MarkAllAsReadAsync(Guid userId);
        Task<bool> MarkAsSentAsync(int notificationId);
        Task<int> DeleteReadOlderThanDaysAsync(int days); // delete read notifications older than specified days
    }
}
