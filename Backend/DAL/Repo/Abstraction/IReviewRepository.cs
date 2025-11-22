namespace DAL.Repo.Abstraction
{
    public interface IReviewRepository : IGenericRepository<Review>
    {
        Task<IEnumerable<Review>> GetReviewsByGuestAsync(Guid guestId);             // All reviews for a specific guest
        Task<IEnumerable<Review>> GetReviewsByBookingAsync(int bookingId);          // All reviews for a specific booking

        // Repository creates and persists review entities
        Task<Review> CreateAsync(int bookingId, Guid guestId, int rating, string comment, DateTime createdAt);
        Task<Review> UpdateAsync(Review review);
        Task<bool> DeleteAsync(Review review);
    }
}