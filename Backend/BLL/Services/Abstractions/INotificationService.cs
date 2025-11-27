namespace BLL.Services.Abstractions
{
    public interface INotificationService 
    {
        Task<Response<List<GetNotificationVM>>> GetByUserIdAsync(Guid userId);
        Task<Response<List<GetNotificationVM>>> GetAllAsync();
        Task<Response<List<GetNotificationVM>>> GetUnreadAsync(Guid userId);
        Task<Response<int>> GetUnreadCountAsync(Guid userId);
        Task<Response<List<GetNotificationVM>>> GetPagedAsync(Guid userId, int page, int pageSize);
        Task<Response<CreateNotificationVM>> CreateAsync(CreateNotificationVM model);
        Task<Response<bool>> MarkAllAsReadAsync(Guid userId);
        Task<Response<bool>> SendPendingNotificationsAsync();
        Task<Response<GetNotificationVM>> GetByIdAsync(int notificationId);
        Task<Response<bool>> MarkAsReadAsync(int notificationId);
        


    }
}
