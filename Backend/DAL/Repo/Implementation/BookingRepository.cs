namespace DAL.Repo.Implementation
{
    public class BookingRepository : GenericRepository<Booking>, IBookingRepository
    {
        public BookingRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<Booking>> GetBookingsByGuestAsync(Guid guestId)
        {
            return await _context.Bookings
                .Where(b => b.GuestId == guestId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Booking>> GetBookingsByListingAsync(int listingId)
        {
            return await _context.Bookings
                .Where(b => b.ListingId == listingId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Booking>> GetActiveBookingsAsync()
        {
            return await _context.Bookings
                .Where(b => b.BookingStatus == BookingStatus.Pending)
                .ToListAsync();
        }

        public async Task<Booking> CreateAsync(int listingId, Guid guestId, DateTime checkInDate, DateTime checkOutDate, decimal totalPrice)
        {
            var entity = Booking.Create(listingId, guestId, checkInDate, checkOutDate, totalPrice);
            await _context.Bookings.AddAsync(entity);
            // Do not call SaveChanges here so caller can control transaction
            return entity;
        }

        // Get booking by id including related Listing, Guest, and Payment for Email notifications
        public async Task<Booking?> GetByIdAsync(int id)
        {
            return await _context.Bookings.FindAsync(id);
        }

        public async Task<Booking?> GetByIdWithListingAndHostAsync(int id)
        {
            return await _context.Bookings
                .Include(b => b.Listing)
                    .ThenInclude(l => l.User) // Host
                .Include(b => b.Guest)
                .Include(b => b.Payment)
                .FirstOrDefaultAsync(b => b.Id == id);
        }
        public async Task<IEnumerable<Booking>> GetBookingsForCheckInReminderAsync(DateTime checkInDate)
        {
            return await _context.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Listing)
                .Where(b => b.CheckInDate.Date == checkInDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Booking>> GetBookingsForCheckOutReminderAsync(DateTime checkOutDate)
        {
            return await _context.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Listing)
                .Where(b => b.CheckOutDate.Date == checkOutDate)
                .ToListAsync();
        }
    }
}
