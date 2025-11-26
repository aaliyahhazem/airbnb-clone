namespace BLL.ModelVM.Email
{
    public class CheckInReminderEmailVM
    {
        public string Email { get; set; } = null!;
        public string GuestName { get; set; } = null!;
        public string ListingTitle { get; set; } = null!;
        public DateTime CheckInDate { get; set; }
    }
}
