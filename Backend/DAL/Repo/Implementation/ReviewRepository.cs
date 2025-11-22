namespace DAL.Repo.Implementation
{
    public class ReviewRepository : GenericRepository<Review>, IReviewRepository
    {
        public ReviewRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<Review>> GetReviewsByGuestAsync(Guid guestId)
        {
            return await _context.Reviews
                .Where(r => r.GuestId == guestId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Review>> GetReviewsByBookingAsync(int bookingId)
        {
            return await _context.Reviews
                .Where(r => r.BookingId == bookingId)
                .ToListAsync();
        }

        public async Task<Review> CreateAsync(int bookingId, Guid guestId, int rating, string comment, DateTime createdAt)
        {
            var entity = Review.Create(bookingId, guestId, rating, comment, createdAt);
            await _context.Reviews.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<Review> UpdateAsync(Review review)
        {
            _context.Reviews.Update(review);
            await _context.SaveChangesAsync();
            return review;
        }

        public async Task<bool> DeleteAsync(Review review)
        {
            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
