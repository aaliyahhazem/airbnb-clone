namespace BLL.Services.Abstractions
{
    public interface IListingImageService
    {
        /// Get all (active) images for a listing as view models.
        /// Used on listing details page.
        Task<Response<List<ListingImageVM>>> GetImagesByListingAsync(int listingId, CancellationToken ct = default);

        /// Get a single image by id (active) as a view model.
        //Task<Response<ListingImageVM?>> GetByIdAsync(int id, CancellationToken ct = default);

        /// Host uploads one or more images for a listing.
        /// Returns list of created image ids.
        /// - Validates listing exists and owner is hostId.
        /// - Uploads files to wwwroot/listings and persist urls.
        Task<Response<int>> AddImagesAsync(int listingId, List<IFormFile> files, Guid hostId, CancellationToken ct = default);

        /// Host updates an existing image (replace file).
        /// - Validates owner
        /// - Uploads new file, deletes old file from disk
        /// - Sets concurrency originalRowVersion if provided (byte[])
        Task<Response<bool>> UpdateImageAsync(int imageId, IFormFile file, Guid hostId, CancellationToken ct = default);

        /// Host soft-deletes their image (owner-only).
        /// Accepts optional originalRowVersion to enable optimistic concurrency check.
        Task<Response<bool>> SoftDeleteImagesAsync(List<int> imageIds, Guid hostId, CancellationToken ct = default);

        /// Admin soft-deletes an image (no owner check).
        /// Accepts optional originalRowVersion.
        //Task<Response<bool>> SoftDeleteByAdminAsync(int imageId, Guid performedByUserId, byte[]? originalRowVersion = null, CancellationToken ct = default);

        /// Set listing's main image (host-only). Delegates to repo that sets main image (listing aggregate).
        /// Accepts optional originalRowVersion to protect listing-level concurrency.
        //Task<Response<bool>> SetMainImageAsync(int listingId, int imageId, Guid hostId, byte[]? originalRowVersion = null, CancellationToken ct = default);
    }
}
