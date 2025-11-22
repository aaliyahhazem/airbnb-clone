
namespace DAL.Repo.Abstraction
{
    public interface IListingRepository : IGenericRepository<Listing>
    {
        Task<int> CreateAsync(
            Listing listing,
            string mainImageUrl,
            List<string>? additionalImageUrls,
            List<string>? keywordNames,
            Guid hostId,
            CancellationToken ct = default);

        Task<bool> UpdateAsync(
            int listingId,
            Guid hostId,
            Listing updatedListing,
            string? newMainImageUrl,
            List<string>? newAdditionalImages,
            List<int>? imagesToRemove,
            List<string>? keywordNames,
            CancellationToken ct = default);

        Task<bool> DeleteAsync(int listingId, Guid hostId, CancellationToken ct = default);

        Task<bool> ApproveAsync(int id, Guid approverUserId, CancellationToken ct = default);

        Task<bool> RejectAsync(int id, Guid approverUserId, string? note = null, CancellationToken ct = default);

        Task<bool> PromoteAsync(int id, DateTime promotionEndDate, Guid performedByUserId, CancellationToken ct = default);

        Task<bool> SetMainImageAsync(int listingId, int imageId, string performedBy, CancellationToken ct = default);

        Task<bool> IsOwnerAsync(int listingId, Guid userId, CancellationToken ct = default);

        Task<Listing?> GetListingByIdAsync(int id, CancellationToken ct = default);

        Task<(IEnumerable<Listing> Listings, int TotalCount)> GetUserViewAsync(
            ListingFilterDto? filter = null,
            int page = 1,
            int pageSize = 10,
            CancellationToken ct = default);

        Task<(IEnumerable<Listing> Listings, int TotalCount)> GetHostViewAsync(
            Guid hostId,
            Expression<Func<Listing, bool>>? filter = null,
            int page = 1,
            int pageSize = 10,
            CancellationToken ct = default);

        Task<(IEnumerable<Listing> Listings, int TotalCount)> GetAdminViewAsync(
            Expression<Func<Listing, bool>>? filter = null,
            int page = 1,
            int pageSize = 10,
            bool includeDeleted = false,
            CancellationToken ct = default);

        Task<(IEnumerable<Listing> Listings, int TotalCount)> GetPendingApprovalsAsync(
            Expression<Func<Listing, bool>>? filter = null,
            int page = 1,
            int pageSize = 10,
            CancellationToken ct = default);
    }
}
