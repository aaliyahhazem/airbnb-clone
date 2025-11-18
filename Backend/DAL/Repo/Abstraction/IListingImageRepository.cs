namespace DAL.Repo.Abstraction
{
    public interface IListingImageRepository : IGenericRepository<ListingImage>
    {
        // Specific operations for ListingImage
        Task<ListingImage?> GetImageByIdAsync(int id, CancellationToken ct = default);// Get image by its ID
        Task<IEnumerable<ListingImage>> GetImagesByListingIdAsync(int listingId, CancellationToken ct = default);
        Task<IEnumerable<ListingImage>> GetActiveImagesByListingIdAsync(int listingId, CancellationToken ct = default);
        Task<ListingImage?> GetMainImageByListingIdAsync(int listingId, CancellationToken ct = default);

        // Batch operations
        Task<int> AddImagesAsync(int listingId, List<string> imageUrls, string createdBy, CancellationToken ct = default);
        Task<bool> SoftDeleteImagesAsync(List<int> imageIds, string deletedBy, CancellationToken ct = default);
        Task<bool> SetMainImageAsync(int listingId, int imageId, string performedBy, CancellationToken ct = default);

        // Validation
        Task<bool> ImageExistsAsync(int imageId, int listingId, CancellationToken ct = default);
        Task<bool> IsImageOwnerAsync(int imageId, Guid userId, CancellationToken ct = default);

        // Filtered queries
        Task<(IEnumerable<ListingImage> Images, int TotalCount)> GetImagesAsync(
            Expression<Func<ListingImage, bool>>? filter = null,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default);
    }
}






    

