namespace PL.Background_Jobs
{
    public class NotificationCleanupJob
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationCleanupJob> _logger;
        private readonly int _retentionDays;

        public NotificationCleanupJob(INotificationService notificationService, 
                                      IConfiguration config, 
                                      ILogger<NotificationCleanupJob> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
            _retentionDays = config.GetValue<int>("BackgroundJobs:NotificationsRetentionDays", 30);
        }
        public async Task ExecuteAsync() 
        { 
            try { 
            var res = await _notificationService.DeleteOldNotificationsAsync(_retentionDays);
            if (!res.IsHaveErrorOrNo)
                _logger.LogInformation("NotificationCleanupJob deleted {Count} notifications", res.result);
                }
            catch (Exception ex)
            {
                _logger.LogError("NotificationCleanupJob failed: {Error}", ex.Message);
            }
        }
    }
}
