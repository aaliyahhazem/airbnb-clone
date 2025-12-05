namespace DAL.Repo.Abstraction
{
    public interface IBookingRepository : IGenericRepository<Booking>
    {
        Task<IEnumerable<Booking>> GetBookingsByGuestAsync(Guid guestId);                // Bookings of a guest
        Task<IEnumerable<Booking>> GetBookingsByListingAsync(int listingId);             // Bookings for a listing
        Task<IEnumerable<Booking>> GetActiveBookingsAsync();                             // All active bookings  
        Task<Booking> CreateAsync(int listingId, Guid guestId, DateTime checkInDate, DateTime checkOutDate, decimal totalPrice); // Create a booking entity and add to context (does not call SaveChanges)

        // Email notification related methods
        Task<Booking?> GetByIdAsync(int id);
        Task<Booking?> GetByIdWithListingAndHostAsync(int id);
        Task<IEnumerable<Booking>> GetBookingsForCheckInReminderAsync(DateTime checkInDate);
        Task<IEnumerable<Booking>> GetBookingsForCheckOutReminderAsync(DateTime checkOutDate);

    }
}