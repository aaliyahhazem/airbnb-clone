namespace PL.Background_Jobs
{
    public class MessageCleanupJob
    {
        private readonly IMessageService _messageService;
        private readonly ILogger<MessageCleanupJob> _logger;
        private readonly int _retentionDays;

        public MessageCleanupJob(IMessageService messageService, IConfiguration config, ILogger<MessageCleanupJob> logger)
        {
            _messageService = messageService;
            _logger = logger;
            _retentionDays = config.GetValue<int>("BackgroundJobs:MessagesRetentionDays", 30);
        }
        public async Task ExecuteAsync()
        {
            _logger.LogInformation("MessageCleanupJob started (retentionDays={Days})", _retentionDays);
            var res = await _messageService.DeleteOldSeenMessagesAsync(_retentionDays);
            if (!res.IsHaveErrorOrNo)
            {
                _logger.LogInformation("MessageCleanupJob deleted {Count} messages", res.result);
            }
            else
            {
                _logger.LogError("MessageCleanupJob failed: {Error}", res.errorMessage);
            }
        }
    }
}
