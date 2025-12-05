namespace DAL.Entities
{
    public class Booking
    {
        public int Id { get; private set; }
        public int ListingId { get; private set; }
        public Guid GuestId { get; private set; }
        public DateTime CheckInDate { get; private set; }
        public DateTime CheckOutDate { get; private set; }
        public decimal TotalPrice { get; private set; }
        public BookingPaymentStatus PaymentStatus { get; private set; }
        public BookingStatus BookingStatus { get; private set; }
        public string? PaymentIntentId { get; private set; }

        public DateTime CreatedAt { get; private set; }

        // Relationships
        public Listing Listing { get; private set; } = null!;
        public User Guest { get; private set; } = null!;
        public Payment? Payment { get; private set; }
        public Review? Review { get; private set; }

        private Booking() { }

        // CreateImage a booking
        internal static Booking Create(
            int listingId,
            Guid guestId,
            DateTime checkInDate,
            DateTime checkOutDate,
            decimal totalPrice)
        {
            if (checkOutDate <= checkInDate)
                throw new ArgumentException("Check-out date must be after check-in date.");

            if (totalPrice < 0)
                throw new ArgumentException("Total price cannot be negative.");

            return new Booking
            {
                ListingId = listingId,
                GuestId = guestId,
                CheckInDate = checkInDate,
                CheckOutDate = checkOutDate,
                TotalPrice = totalPrice,
                PaymentStatus = BookingPaymentStatus.Pending,
                BookingStatus = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
        }

        // Update existing booking
        public void Update(
            DateTime checkInDate,
            DateTime checkOutDate,
            decimal totalPrice,
            BookingPaymentStatus paymentStatus,
            BookingStatus bookingStatus)
        {
            if (checkOutDate <= checkInDate)
                throw new ArgumentException("Check-out date must be after check-in date.");

            if (totalPrice < 0)
                throw new ArgumentException("Total price cannot be negative.");

            CheckInDate = checkInDate;
            CheckOutDate = checkOutDate;
            TotalPrice = totalPrice;
            PaymentStatus = paymentStatus;
            BookingStatus = bookingStatus;
        }
        // Set PaymentIntentId  
        public void SetPaymentIntentId(string paymentIntentId)
        {
            PaymentIntentId = paymentIntentId;
        }
    }
}
