using DAL.Entities;

namespace DAL.Repo.Abstraction
{
    public interface IListingImageRepository : IGenericRepository<ListingImage>
    {
        Task<ListingImage?> GetImageByIdAsync(int id, CancellationToken ct = default);

        Task<IEnumerable<ListingImage>> GetActiveImagesByListingIdAsync(int listingId, CancellationToken ct = default);

        Task<int> AddImagesAsync(int listingId, List<string> imageUrls, string createdBy, CancellationToken ct = default);

        Task<bool> SoftDeleteImagesAsync(List<int> imageIds, string deletedBy, CancellationToken ct = default);

        Task<bool> SetMainImageAsync(int listingId, int imageId, string performedBy, CancellationToken ct = default);

        Task<bool> IsImageOwnerAsync(int imageId, Guid userId, CancellationToken ct = default);

        Task<bool> HardDeleteImageById(int imageId, string deletedBy, CancellationToken ct = default);
    }
}
