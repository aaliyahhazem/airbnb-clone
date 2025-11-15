using System.Linq.Expressions;

public interface IListingRepository : IGenericRepository<Listing>
{
    //cancellation token : allows an operation to be canceled before it finishes

    // Overview: returns listings including only MainImage (fast) in overview view that appear on main page
    Task<IEnumerable<Listing>> GetPagedOverviewAsync(int page, int pageSize, Expression<Func<Listing, bool>>? filter = null, CancellationToken ct = default);

    // Details: returns listing with full images collection in details view
    Task<Listing?> GetByIdWithImagesAsync(int id, CancellationToken ct = default);

    Task<Listing?> GetByIdAsync(int id, CancellationToken ct = default); // general fetch (no includes) without images
    Task<bool> ExistsAsync(int id, CancellationToken ct = default); // check existence by id

    // ---------- Host ----------
   
    Task<int> AddAsync(Listing listing,Guid hostId, CancellationToken ct = default);// creates new listing
    Task<bool> UpdateAsync(int listingId, Guid hostId, Listing listing, CancellationToken ct = default); // saves and returns success
    Task<IEnumerable<Listing>> GetByUserAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);// paged listings by owner
    Task<bool> SoftDeleteByOwnerAsync(int listingId, Guid ownerUserId, CancellationToken ct = default);// soft delete by owner
    Task<bool> IsOwnerAsync(int listingId, Guid userId, CancellationToken ct = default);// check if user is owner of listing

    // ---------- Admin ----------
    Task<bool> SoftDeleteAsync(int id, Guid performedByUserId, CancellationToken ct = default);// soft delete by admin
    Task<bool> ApproveAsync(int id, Guid approverUserId, CancellationToken ct = default);// approve listing
    Task<bool> RejectAsync(int id, Guid approverUserId, string? note = null, CancellationToken ct = default);// reject listing
    Task<bool> PromoteAsync(int id, DateTime promotionEndDate, CancellationToken ct = default);// promote listing
    Task<Listing?> GetByIdAdminAsync(int id, bool includeDeleted = false, CancellationToken ct = default); // fetch by id for admin with option to include deleted listings

   

}
