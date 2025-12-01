namespace BLL.ModelVM.Payment
{
    public class CreatePaymentIntentVm
    {
        public string ClientSecret { get; set; }
        public string PaymentIntentId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "usd";
    }
}
