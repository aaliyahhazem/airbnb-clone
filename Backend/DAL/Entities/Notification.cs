using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Entities
{
    public class Notification
    {
        public int Id { get; private set; }
        
        public Guid UserId { get; private set; }
        public string Title { get; private set; } = null!;
        public string Body { get; private set; } = null!;
        public NotificationType Type { get; private set; }
        public bool IsRead { get; private set; } = false;
        public bool IsSentViaFCM { get; private set; } =false;
        public DateTime CreatedAt { get; private set; }

        public DateTime? DeletedOn { get; private set; }
        public DateTime? UpdatedOn { get; private set; }
        public string? CreatedBy { get; private set; }
        public string? UpdatedBy { get; private set; }
        public string? DeletedBy { get; private set; }
        public bool IsDeleted { get; private set; } = false;

        // Relationships
        public User User { get; private set; } = null!;

        private Notification() { }

        // CreateImage a notification
        internal static Notification Create(
            Guid userId,
            string title,
            string body,
            NotificationType type)
        {
            return new Notification
            {
                UserId = userId,
                Title = title,
                Body = body,
                Type = type,
                IsRead = false,
                IsSentViaFCM = false,
                CreatedAt = DateTime.UtcNow
            };
        }

        // Mark the notification as read
        internal void MarkAsRead()
        {
            IsRead = true;
        }

        // Mark the notification as sent via FCM
        internal void MarkAsSent()
        {
            IsSentViaFCM = true;
        }
    }
}
