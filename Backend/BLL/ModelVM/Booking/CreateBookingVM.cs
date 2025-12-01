namespace BLL.ModelVM.Booking
{
    public class CreateBookingVM
    {
        public int ListingId { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int Guests { get; set; }
        public string PaymentMethod { get; set; } = "stripe";
    }
}