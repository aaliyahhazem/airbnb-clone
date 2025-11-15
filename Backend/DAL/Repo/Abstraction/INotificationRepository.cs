namespace DAL.Repo.Abstraction
{
    public interface INotificationRepository : IGenericRepository<Notification>
    {

        Task<Notification?> GetByIdAsync(int id);
        Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<Notification>> GetUnreadAsync(Guid userId);
        Task<IEnumerable<Notification>> GetPagedAsync(Guid userId, int page, int pageSize);
        Task<IEnumerable<Notification>> GetPendingToSendAsync();

        Task<Notification> CreateAsync(Notification notification);
        Task<Notification> UpdateAsync(Notification notification);
        Task <bool> DeleteAsync(Notification notification);

        Task<bool> MarkAsReadAsync(int notificationId);
        Task<bool> MarkAllAsReadAsync(Guid userId);
        Task<bool> MarkAsSentAsync(int notificationId);
    }
}
