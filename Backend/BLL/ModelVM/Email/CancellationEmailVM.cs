namespace BLL.ModelVM.Email
{
    public class CancellationEmailVM
    {
        public string Email { get; set; } = null!;
        public string GuestName { get; set; } = null!;
        public string ListingTitle { get; set; } = null!;
        public DateTime CheckIn { get; set; }
        public DateTime CancelledAt { get; set; }
        public bool CancelledByHost { get; set; }
    }
}
