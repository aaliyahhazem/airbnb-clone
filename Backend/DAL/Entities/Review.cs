namespace DAL.Entities
{
    public class Review
    {
        public int Id { get; private set; }
        public int BookingId { get; private set; }
        public Guid GuestId { get; set; } 
        public int Rating { get; private set; }
        public string Comment { get; private set; } = null!;
        public DateTime CreatedAt { get; private set; }

        // Relationships
        public Booking? Booking { get; private set; }
        public User? Guest { get; private set; }

        private Review() { }
         
        // Create a review
        public static Review Create(
            int bookingId,
            Guid guestId,
            int rating,
            string comment,
            DateTime createdAt)
        {
            return new Review
            {
                BookingId = bookingId,
                GuestId = guestId,
                Rating = rating,
                Comment = comment,
                CreatedAt = createdAt
            };
        }
         
        // Update existing review
        public void Update(
            int rating,
            string comment)
        {
            Rating = rating;
            Comment = comment;
        }
    }
}