namespace DAL.Repo.Implementation
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;

            // Initialize repos
            Users = new UserRepository(_context);
            Listings = new ListingRepository(_context);
            Bookings = new BookingRepository(_context);
            Payments = new PaymentRepository(_context);
            Reviews = new ReviewRepository(_context);
            Messages = new MessageRepository(_context);
            Notifications = new NotificationRepository(_context);
            ListingImages = new ListingImageRepository(_context);
            Amenities = new AmenityRepository(_context);
        }

        public IUserRepository Users { get; private set; }
        public IListingRepository Listings { get; private set; }
        public IBookingRepository Bookings { get; private set; }
        public IPaymentRepository Payments { get; private set; }
        public IReviewRepository Reviews { get; private set; }
        public IMessageRepository Messages { get; private set; }
        public INotificationRepository Notifications { get; private set; }
        public IListingImageRepository ListingImages { get; private set; }
        public IAmenityRepository Amenities { get; private set; }
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
