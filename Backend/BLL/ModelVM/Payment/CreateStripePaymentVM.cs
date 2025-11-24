namespace BLL.ModelVM.Payment
{
    public class CreateStripePaymentVM
    {
        public int BookingId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "usd";
        public string? Description { get; set; }
    }
}
