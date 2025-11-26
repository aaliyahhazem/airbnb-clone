namespace BLL.ModelVM.Email
{
    public class PaymentReceiptVM
    {
        public string Email { get; set; } = null!;
        public string GuestName { get; set; } = null!;
        public decimal Amount { get; set; }
        public DateTime PaidAt { get; set; }
        public string PaymentMethod { get; set; } = null!;
    }
}
