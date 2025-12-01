
namespace DAL.Repo.Abstraction
{
    public interface IListingRepository : IGenericRepository<Listing>
    {
        // Creates a new listing along with its images and keywords, returning the new listing's ID.
        Task<int> CreateAsync(Listing listing,string mainImageUrl,List<string>? additionalImageUrls,List<string>? keywordNames,Guid hostId,CancellationToken ct = default);
        
        // Updates an existing listing and its associated images and keywords.
        Task<bool> UpdateAsync(int listingId,Guid hostId,Listing updatedListing,string? newMainImageUrl,List<string>? newAdditionalImages,List<int>? imagesToRemove,List<string>? keywordNames,CancellationToken ct = default);

        // Deletes a listing owned by the specified host.
        Task<bool> DeleteAsync(int listingId, Guid hostId, CancellationToken ct = default);


        // Approves a listing for publication.by admin
        Task<bool> ApproveAsync(int id, Guid approverUserId, CancellationToken ct = default);

        // Rejects a listing with an optional note.by admin
        Task<bool> RejectAsync(int id, Guid approverUserId, string? note = null, CancellationToken ct = default);

        // Sets the main image for a listing by its ID.
        Task<bool> SetMainImageAsync(int listingId, int imageId, string performedBy, CancellationToken ct = default);

        // Checks if a user is the owner of a specific listing.
        Task<bool> IsOwnerAsync(int listingId, Guid userId, CancellationToken ct = default);

        Task<Listing?> GetListingByIdAsync(int id, CancellationToken ct = default);

        // Gets listings for user view with optional filtering and pagination.
        Task<(IEnumerable<Listing> Listings, int TotalCount)> GetUserViewAsync(ListingFilterDto? filter = null,int page = 1,int pageSize = 10,CancellationToken ct = default);


        // Gets listings for host view with optional filtering and pagination.
        Task<(IEnumerable<Listing> Listings, int TotalCount)> GetHostViewAsync(Guid hostId,Expression<Func<Listing, bool>>? filter = null,int page = 1,int pageSize = 10,CancellationToken ct = default);

        // Gets listings for admin view with optional filtering, pagination, and inclusion of deleted listings.
        Task<(IEnumerable<Listing> Listings, int TotalCount)> GetAdminViewAsync(Expression<Func<Listing, bool>>? filter = null,int page = 1,int pageSize = 10,bool includeDeleted = false,CancellationToken ct = default);


        // Gets listings pending approval with optional filtering and pagination for admin review.
        Task<(IEnumerable<Listing> Listings, int TotalCount)> GetPendingApprovalsAsync(Expression<Func<Listing, bool>>? filter = null,int page = 1,int pageSize = 10,CancellationToken ct = default);

        // Promotes a listing until the specified end date.by admin
        Task<bool> PromoteAsync(int id, DateTime promotionEndDate, Guid performedByUserId, CancellationToken ct = default);

        // Unpromotes a listing.by admin    
        Task<bool> UnpromoteAsync(int id, Guid performedByUserId, CancellationToken ct = default);

        // Extends the promotion end date of a listing.by admin
        public Task<bool> ExtendPromotionAsync(int id, DateTime newPromotionEndDate, Guid performedByUserId, CancellationToken ct = default);

        // Priority Management Methods
        Task<bool> IncrementViewPriorityAsync(int listingId, CancellationToken ct = default);
        Task<bool> IncrementFavoritePriorityAsync(int listingId, CancellationToken ct = default);
        Task<bool> DecrementFavoritePriorityAsync(int listingId, CancellationToken ct = default);
        Task<bool> IncrementBookingPriorityAsync(int listingId, CancellationToken ct = default);
        Task<bool> AdjustPriorityByReviewRatingAsync(int listingId, int rating, CancellationToken ct = default);
        Task<bool> ReverseRatingPriorityAdjustmentAsync(int listingId, int oldRating, CancellationToken ct = default);

    }
}
