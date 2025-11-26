namespace DAL.Repo.Abstraction
{
    public interface IUnitOfWork : IDisposable
    {
        IUserRepository Users { get; }
        IListingRepository Listings { get; }
        IBookingRepository Bookings { get; }
        IPaymentRepository Payments { get; }
        IReviewRepository Reviews { get; }
        IMessageRepository Messages { get; }
        INotificationRepository Notifications { get; }
        IListingImageRepository ListingImages { get; }
        IAmenityRepository Amenities { get; }
        IFavoriteRepository Favorites { get; }

        // Commits all changes in one transaction
        Task<int> SaveChangesAsync(); 

        // Execute multiple operations within a single database transaction
        Task ExecuteInTransactionAsync(Func<Task> operation);
    }
}
