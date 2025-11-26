namespace BLL.ModelVM.Email
{
    public class CheckOutReminderEmailVM
    {
        public string Email { get; set; } = null!;
        public string GuestName { get; set; } = null!;
        public string ListingTitle { get; set; } = null!;
        public DateTime CheckOutDate { get; set; }
    }
}
