using DAL.Enum;

namespace BLL.ModelVM.Notification
{
    public class CreateNotificationVM
    {
        public int? Id { get; set; }
        // Server will set this from token; clients should not provide it.
        public Guid UserId { get; set; }
        public string Title { get; set; } = null!;
        public string Body { get; set; } = null!;
        public NotificationType Type { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
