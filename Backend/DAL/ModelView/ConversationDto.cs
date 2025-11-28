namespace DAL.Dto
{
    public class ConversationDto
    {
        public Guid OtherUserId { get; set; }
        public string OtherUserName { get; set; } = null!;
        public string? LastMessage { get; set; }
        public DateTime? LastSentAt { get; set; }
        public int UnreadCount { get; set; }
    }
}
