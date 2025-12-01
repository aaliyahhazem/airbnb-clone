namespace BLL.ModelVM.Booking
{
    public class GetBookingVM
    {
        public int Id { get; set; }
        public int ListingId { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public decimal TotalPrice { get; set; }
        public string BookingStatus { get; set; } = null!;
        public string PaymentStatus { get; set; } = null!;
        public string? ClientSecret { get; set; }
        public string? PaymentIntentId { get; set; }
    }
}