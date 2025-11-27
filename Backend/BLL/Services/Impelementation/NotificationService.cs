
namespace BLL.Services.Impelementation
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;


        public NotificationService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // CreateImage
        public async Task<Response<CreateNotificationVM>> CreateAsync(CreateNotificationVM model)
        {
            try
            {

                var entity = _mapper.Map<Notification>(model);


                var result = await _unitOfWork.Notifications.CreateAsync(entity);
                await _unitOfWork.SaveChangesAsync();

                if (result != null)
                    return new Response<CreateNotificationVM>(model, null, false);

                return new Response<CreateNotificationVM>(null, "Failed to create notification", true);
            }
            catch (Exception ex)
            {
                return new Response<CreateNotificationVM>(null, ex.Message, true);
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
        public async Task<Response<bool>> MarkAsReadAsync(int notificationId)
        {
            try
            {
                var ok = await _unitOfWork.Notifications.MarkAsReadAsync(notificationId);
                await _unitOfWork.SaveChangesAsync();

                if (!ok)
                    return new Response<bool>(false, "Failed to mark notification as read", true);

                return new Response<bool>(true, null, false);
            }
            catch (Exception ex)
            {
                return new Response<bool>(false, ex.Message, true);
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
    }
}
