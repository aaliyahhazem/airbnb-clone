namespace BLL.ModelVM.Email
{
    public class PayoutNotificationVM
    {
        public string Email { get; set; } = null!;
        public string HostName { get; set; } = null!;
        public string ListingTitle { get; set; } = null!;
        public decimal Amount { get; set; }
        public DateTime PayoutDate { get; set; }
        public string TransactionId { get; set; } = null!;
    }

}
