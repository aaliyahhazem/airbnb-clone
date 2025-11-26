namespace BLL.ModelVM.Email
{
    public class HostNewBookingVM
    {
        public string HostEmail { get; set; } = null!;
        public string HostName { get; set; } = null!;
        public string GuestName { get; set; } = null!;
        public string ListingTitle { get; set; } = null!;
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
    }
}