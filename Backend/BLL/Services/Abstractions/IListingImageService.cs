
namespace BLL.Services.Abstractions
{
    public interface IListingImageService
    {
        /// Get all active images for a listing.
        Task<Response<List<ListingImageVM>>> GetImagesByListingAsync(int listingId, CancellationToken ct = default);

        /// Host uploads one or more images for a listing.
        Task<Response<int>> AddImagesAsync(int listingId, List<IFormFile> files, Guid hostId, CancellationToken ct = default);

        /// Host updates an existing image (replace file).
        Task<Response<bool>> UpdateImageAsync(int imageId, IFormFile file, Guid hostId, CancellationToken ct = default);

        /// Host soft-deletes one or more of their images.
        Task<Response<bool>> SoftDeleteImagesAsync(List<int> imageIds, Guid hostId, CancellationToken ct = default);

        /// Admin soft-deletes an image (no owner check).
        Task<Response<bool>> SoftDeleteByAdminAsync(int imageId, Guid performedByUserId, CancellationToken ct = default);

        /// Host sets listing's main image.
        Task<Response<bool>> SetMainImageAsync(int listingId, int imageId, Guid hostId, CancellationToken ct = default);

        /// Hard delete specific image by id (host only).
        Task<Response<bool>> DeleteImageByIdAsync(int imageId, Guid hostId, CancellationToken ct = default);
    }
}
