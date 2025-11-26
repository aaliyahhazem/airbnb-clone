namespace BLL.ModelVM.Email
{
    public class BookingConfirmationVM
    {
        public string Email { get; set; } = null!;
        public string GuestName { get; set; } = null!;
        public string ListingTitle { get; set; } = null!;
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public decimal TotalPrice { get; set; }
    }

}
