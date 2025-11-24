namespace BLL.ModelVM.Payment
{
    public class CreatePaymentIntentVm
    {
        public string ClientSecret { get; set; } = null!;
        public string PaymentIntentId { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "usd";
    }
}
