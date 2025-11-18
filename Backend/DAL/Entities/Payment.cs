namespace DAL.Entities
{
    public class Payment
    {
        public int Id { get; private set; }
        public int BookingId { get; private set; }
        public decimal Amount { get; private set; }
        public string PaymentMethod { get; private set; } = null!;
        public string TransactionId { get; private set; } = null!;
        public PaymentStatus? Status { get; private set; }
        public DateTime PaidAt { get; private set; }

        // Relationships
        public Booking Booking { get; private set; } = null!;

        private Payment() { }

        // CreateImage a payment
        internal static Payment Create(
            int bookingId,
            decimal amount,
            string paymentMethod,
            string transactionId,
            PaymentStatus? status,
            DateTime paidAt)
        {
            return new Payment
            {
                BookingId = bookingId,
                Amount = amount,
                PaymentMethod = paymentMethod,
                TransactionId = transactionId,
                Status = status,
                PaidAt = paidAt
            };
        }

        // Update existing payment
        internal void Update(
            decimal amount,
            string paymentMethod,
            string transactionId,
            PaymentStatus? status,
            DateTime paidAt)
        {
            Amount = amount;
            PaymentMethod = paymentMethod;
            TransactionId = transactionId;
            Status = status;
            PaidAt = paidAt;
        }
    }
}
