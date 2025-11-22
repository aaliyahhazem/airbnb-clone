namespace BLL.ModelVM.Message
{
    public class CreateMessageVM
    {
        public Guid ReceiverId { get; set; }
        public string Content { get; set; } = null!;
    }
}
