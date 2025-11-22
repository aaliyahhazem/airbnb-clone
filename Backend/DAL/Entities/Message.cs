namespace DAL.Entities
{
    public class Message
    {
        public int Id { get; private set; }
        public Guid SenderId { get; private set; }
        public Guid ReceiverId { get; private set; }
        public string Content { get; private set; } = null!;
        public DateTime SentAt { get; private set; }
        public bool IsRead { get; private set; }

        // Relationships
        public User Sender { get; private set; } = null!;
        public User Receiver { get; private set; } = null!;

        private Message() { }

        // Create a new message
        internal static Message Create(
            Guid senderId,
            Guid receiverId,
            string content,
            DateTime sentAt,
            bool isRead)
        {
            return new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content,
                SentAt = sentAt,
                IsRead = isRead
            };
        }

        // Update existing message
        internal void Update(
            string content,
            bool isRead)
        {
            Content = content;
            IsRead = isRead;
        }
    }
}