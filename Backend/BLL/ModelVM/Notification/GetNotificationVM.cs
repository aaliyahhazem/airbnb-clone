

namespace BLL.ModelVM.Notification
{
    public class GetNotificationVM
    {
        public int Id { get; set; }

        public Guid UserId { get; set; }
        public string Title { get; set; } = null!;
        public string Body { get; set; } = null!;
    }
}
