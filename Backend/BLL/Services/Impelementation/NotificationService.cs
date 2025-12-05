
namespace BLL.Services.Impelementation
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly Abstractions.INotificationPublisher? _publisher;


        public NotificationService(IUnitOfWork unitOfWork, IMapper mapper, Abstractions.INotificationPublisher? publisher = null)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _publisher = publisher;
        }

        // CreateImage 
        public async Task<Response<GetNotificationVM>> CreateAsync(CreateNotificationVM model)
        {
            try
            {
                var entity = _mapper.Map<Notification>(model);

                var result = await _unitOfWork.Notifications.CreateAsync(entity);
                await _unitOfWork.SaveChangesAsync();

                if (result != null)
                {
                    var mapped = _mapper.Map<GetNotificationVM>(result);
                    // publish via hub if available
                    try
                    {
                        if (_publisher != null)
                        {
                            await _publisher.PublishAsync(mapped);
                        }
                    }
                    catch { /* swallow publisher errors */ }

                    return new Response<GetNotificationVM>(mapped, null, false);
                }

                return new Response<GetNotificationVM>(null, "Failed to create notification", true);
            }
            catch (Exception ex)
            {
                return new Response<GetNotificationVM>(null, ex.Message, true);
            }
        }

        // Get by id
        public async Task<Response<GetNotificationVM>> GetByIdAsync(int notificationId)
        {
            try
            {
                var result = await _unitOfWork.Notifications.GetByIdAsync(notificationId);
                if (result == null)
                    return new Response<GetNotificationVM>(null, "Notification not found", true);

                var mapped = _mapper.Map<GetNotificationVM>(result);
                return new Response<GetNotificationVM>(mapped, null, false);
            }
            catch (Exception ex)
            {
                return new Response<GetNotificationVM>(null, ex.Message, true);
            }
        }

        // Get All for User
        public async Task<Response<List<GetNotificationVM>>> GetByUserIdAsync(Guid userId)
        {
            try
            {
                var result = await _unitOfWork.Notifications.GetByUserIdAsync(userId);
                var mapped = _mapper.Map<List<GetNotificationVM>>(result);

                return new Response<List<GetNotificationVM>>(mapped, null, false);
            }
            catch (Exception ex)
            {
                return new Response<List<GetNotificationVM>>(null, ex.Message, true);
            }
        }

        // Get unread count
        public async Task<Response<int>> GetUnreadCountAsync(Guid userId)
        {
            try
            {
                var count = await _unitOfWork.Notifications.GetUnreadCountAsync(userId);
                return Response<int>.SuccessResponse(count);
            }
            catch (Exception ex)
            {
                return Response<int>.FailResponse(ex.Message);
            }
        }

        // Pagination
        public async Task<Response<List<GetNotificationVM>>> GetPagedAsync(Guid userId, int page, int pageSize)
        {
            try
            {
                var result = await _unitOfWork.Notifications.GetPagedAsync(userId, page, pageSize);
                var mapped = _mapper.Map<List<GetNotificationVM>>(result);

                return new Response<List<GetNotificationVM>>(mapped, null, false);
            }
            catch (Exception ex)
            {
                return new Response<List<GetNotificationVM>>(null, ex.Message, true);
            }
        }

        // Get unread
        public async Task<Response<List<GetNotificationVM>>> GetUnreadAsync(Guid userId)
        {
            try
            {
                var result = await _unitOfWork.Notifications.GetUnreadAsync(userId);
                var mapped = _mapper.Map<List<GetNotificationVM>>(result);

                return new Response<List<GetNotificationVM>>(mapped, null, false);
            }
            catch (Exception ex)
            {
                return new Response<List<GetNotificationVM>>(null, ex.Message, true);
            }
        }

        // Mark one as read
        public async Task<Response<GetNotificationVM>> MarkAsReadAsync(int notificationId)
        {
            try
            {
                var notification = await _unitOfWork.Notifications.MarkAsReadAsync(notificationId);
                await _unitOfWork.SaveChangesAsync();

                if (notification == null)
                    return new Response<GetNotificationVM>(null, "Failed to mark notification as read", true);

                var mapped = _mapper.Map<GetNotificationVM>(notification);
                return new Response<GetNotificationVM>(mapped, null, false);
            }
            catch (Exception ex)
            {
                return new Response<GetNotificationVM>(null, ex.Message, true);
            }
        }

        // Mark all as read
        public async Task<Response<bool>> MarkAllAsReadAsync(Guid userId)
        {
            try
            {
                var ok = await _unitOfWork.Notifications.MarkAllAsReadAsync(userId);
                await _unitOfWork.SaveChangesAsync();

                if (!ok)
                    return new Response<bool>(false, "Failed to mark all as read", true);

                return new Response<bool>(true, null, false);
            }
            catch (Exception ex)
            {
                return new Response<bool>(false, ex.Message, true);
            }
        }

        // Send pending (cron job)
        public async Task<Response<bool>> SendPendingNotificationsAsync()
        {
            try
            {
                await _unitOfWork.Notifications.GetPendingToSendAsync();
                await _unitOfWork.SaveChangesAsync();

                return new Response<bool>(true, null, false);
            }
            catch (Exception ex)
            {
                return new Response<bool>(false, ex.Message, true);
            }
        }

        // Get all notifications (admin)
        public async Task<Response<List<GetNotificationVM>>> GetAllAsync()
        {
            try
            {
                var result = await _unitOfWork.Notifications.GetAllAsync();
                var mapped = _mapper.Map<List<GetNotificationVM>>(result);
                return new Response<List<GetNotificationVM>>(mapped, null, false);
            }
            catch (Exception ex)
            {
                return new Response<List<GetNotificationVM>>(null, ex.Message, true);
            }
        }
        public async Task<Response<int>> DeleteOldNotificationsAsync(int days)
        {
            try
            {
                var deletedCount = await _unitOfWork.Notifications.DeleteReadOlderThanDaysAsync(days);
                return Response<int>.SuccessResponse(deletedCount);
            }
            catch (Exception ex)
            {
                return Response<int>.FailResponse(ex.Message);
            }
        }

    }
}
