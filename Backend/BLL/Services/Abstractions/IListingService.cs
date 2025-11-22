using BLL.ModelVM.ListingVM;
using DAL.Entities;

namespace BLL.Services.Abstractions
{
    public interface IListingService
    {
        /// Create a new listing. Uploads files, builds entity and returns new listing Id.
        Task<Response<int>> CreateAsync(ListingCreateVM vm, Guid hostId, CancellationToken ct = default);

        /// Update a listing (host only). Handles image uploads, removals and concurrency via RowVersionBase64 in VM.
        Task<Response<ListingUpdateVM>> UpdateAsync(int listingId, Guid hostId, ListingUpdateVM vm, CancellationToken ct = default);

        /// Get paged overview for public/home/search (returns overview VMs).
        Task<Response<List<ListingOverviewVM>>> GetPagedOverviewAsync(int page, int pageSize, ListingFilterDto? filter = null, CancellationToken ct = default);

        /// Get listing details including all images.
        Task<Response<ListingDetailVM?>> GetByIdWithImagesAsync(int id, CancellationToken ct = default);

        /// Get paged listings for a host (owner dashboard).
        Task<Response<List<ListingOverviewVM>>> GetByUserAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);

        /// Soft-delete a listing by its owner (host).
        Task<Response<bool>> SoftDeleteByOwnerAsync(int listingId, Guid hostId, CancellationToken ct = default);

        /// Admin approves a listing.
        Task<Response<bool>> ApproveAsync(int id, Guid approverUserId, CancellationToken ct = default);

        /// Admin rejects a listing with an optional note.
        Task<Response<bool>> RejectAsync(int id, Guid approverUserId, string? note, CancellationToken ct = default);

        /// Admin or owner promotes a listing (sets promotion end date).
        Task<Response<bool>> PromoteAsync(int id, DateTime promotionEndDate, Guid performedByUserId, CancellationToken ct = default);

        /// Host sets the main image for a listing (owner-only).
        Task<Response<bool>> SetMainImageAsync(int listingId, int imageId, Guid hostId, CancellationToken ct = default);
    }
}
