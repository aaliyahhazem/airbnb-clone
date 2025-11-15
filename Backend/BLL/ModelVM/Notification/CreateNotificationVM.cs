

using DAL.Enum;

namespace BLL.ModelVM.Notification
{
    public class CreateNotificationVM
    {
        public Guid UserId { get; set; }
        public string Title { get; set; } = null!;
        public string Body { get; set; } = null!;
        public NotificationType Type { get; set; }
    }
}
