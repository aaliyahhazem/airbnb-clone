namespace DAL.Repo.Implementation
{
    public class ListingRepository : GenericRepository<Listing>, IListingRepository
    {
        private readonly AppDbContext _context;

        public ListingRepository(AppDbContext context) : base(context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// Resolve the user's full name from Users table (fallback to Guid string).
        private async Task<string> GetFullNameAsync(Guid userId, CancellationToken ct = default)
        {
            var name = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(ct);

            return string.IsNullOrWhiteSpace(name) ? userId.ToString() : name;
        }

        /// Create a listing aggregate with optional main image and additional images. Returns created Id.
        public async Task<int> CreateAsync(
            Listing listing,
            string mainImageUrl,
            List<string>? additionalImageUrls,
            Guid hostId,
            CancellationToken ct = default)
        {
            try
            {
                if (listing == null) throw new ArgumentNullException(nameof(listing));
                if (hostId == Guid.Empty) throw new ArgumentException(nameof(hostId));

                var hostFullName = await GetFullNameAsync(hostId, ct) ?? hostId.ToString();

                var newListing = Listing.Create(
                    title: listing.Title,
                    description: listing.Description,
                    pricePerNight: listing.PricePerNight,
                    location: listing.Location,
                    latitude: listing.Latitude,
                    longitude: listing.Longitude,
                    maxGuests: listing.MaxGuests,
                    tags: listing.Tags ?? new List<string>(),
                    userId: hostId,
                    createdBy: hostFullName,
                    mainImageUrl: mainImageUrl,
                    isPromoted: listing.IsPromoted,
                    promotionEndDate: listing.PromotionEndDate
                );

                if (additionalImageUrls?.Any() == true)
                {
                    foreach (var url in additionalImageUrls.Where(u => !string.IsNullOrWhiteSpace(u)))
                    {
                        newListing.AddImage(url!, hostFullName);
                    }
                }

                await _context.Listings.AddAsync(newListing, ct);
                await _context.SaveChangesAsync(ct);
                return newListing.Id;
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating listing in repository.", ex);
            }
        }

        /// Update listing (owner only). Uses domain Update and image add/remove operations.
        public async Task<bool> UpdateAsync(
            int listingId,
            Guid hostId,
            Listing updatedListing,
            string? newMainImageUrl,
            List<string>? newAdditionalImages,
            List<int>? imagesToRemove,
            CancellationToken ct = default)
        {
            try
            {
                if (updatedListing == null) throw new ArgumentNullException(nameof(updatedListing));

                var hostFullName = await GetFullNameAsync(hostId, ct) ?? hostId.ToString();

                var entity = await _context.Listings
                    .Include(l => l.Images)
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(l => l.Id == listingId, ct);

                if (entity == null) return false;
                if (entity.IsDeleted) throw new InvalidOperationException("Cannot update a deleted listing.");
                if (entity.UserId != hostId) throw new UnauthorizedAccessException("You are not authorized to update this listing.");

                if (imagesToRemove?.Any() == true)
                {
                    foreach (var imageId in imagesToRemove)
                    {
                        var image = entity.Images.FirstOrDefault(img => img.Id == imageId);
                        image?.SoftDelete(hostFullName);
                    }
                }

                var updated = entity.Update(
                    updatedListing.Title,
                    updatedListing.Description,
                    updatedListing.PricePerNight,
                    updatedListing.Location,
                    updatedListing.Latitude,
                    updatedListing.Longitude,
                    updatedListing.MaxGuests,
                    hostFullName,
                    updatedListing.IsPromoted,
                    updatedListing.PromotionEndDate,
                    updatedListing.Tags
                );

                if (!updated) return false;

                if (newAdditionalImages?.Any() == true)
                {
                    foreach (var url in newAdditionalImages.Where(u => !string.IsNullOrWhiteSpace(u)))
                    {
                        entity.AddImage(url!, hostFullName);
                    }
                }

                if (!string.IsNullOrWhiteSpace(newMainImageUrl))
                {
                    var match = entity.Images.FirstOrDefault(i => i.ImageUrl == newMainImageUrl && !i.IsDeleted);
                    if (match != null)
                        entity.SetMainImage(match, hostFullName);
                    else
                    {
                        entity.AddImage(newMainImageUrl!, hostFullName);
                        var added = entity.Images.OrderByDescending(i => i.CreatedAt).FirstOrDefault();
                        if (added != null) entity.SetMainImage(added, hostFullName);
                    }
                }

                await _context.SaveChangesAsync(ct);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Error updating listing in repository.", ex);
            }
        }

        /// Owner-only soft-delete of listing.
        public async Task<bool> DeleteAsync(int listingId, Guid hostId, CancellationToken ct = default)
        {
            try
            {
                var hostFullName = await GetFullNameAsync(hostId, ct) ?? hostId.ToString();

                var entity = await _context.Listings.FirstOrDefaultAsync(l => l.Id == listingId, ct);
                if (entity == null) return false;
                if (entity.UserId != hostId) throw new UnauthorizedAccessException("You are not authorized to delete this listing.");

                var deleted = entity.SoftDelete(hostFullName);
                if (!deleted) return false;

                await _context.SaveChangesAsync(ct);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Error deleting listing in repository.", ex);
            }
        }

        /// Public user view: only approved & not deleted listings.
        /// Returns (items, totalCount).
        public async Task<(IEnumerable<Listing> Listings, int TotalCount)> GetUserViewAsync(
            Expression<Func<Listing, bool>>? filter = null,
            int page = 1,
            int pageSize = 10,
            CancellationToken ct = default)
        {
            try
            {
                var query = _context.Listings
                    .Where(l => !l.IsDeleted && l.IsApproved)
                    .Include(l => l.MainImage)
                    .Include(l => l.Images.Where(img => !img.IsDeleted))
                    .AsQueryable();

                if (filter != null) query = query.Where(filter);

                var totalCount = await query.CountAsync(ct);

                var items = await query
                    .OrderByDescending(l => l.IsPromoted)
                    .ThenByDescending(l => l.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .AsNoTracking()
                    .ToListAsync(ct);

                return (items, totalCount);
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching user view listings in repository.", ex);
            }
        }

        /// Host view: listings owned by host (not deleted).
        public async Task<(IEnumerable<Listing> Listings, int TotalCount)> GetHostViewAsync(
            Guid hostId,
            Expression<Func<Listing, bool>>? filter = null,
            int page = 1,
            int pageSize = 10,
            CancellationToken ct = default)
        {
            try
            {
                var query = _context.Listings
                    .Where(l => !l.IsDeleted && l.UserId == hostId)
                    .Include(l => l.MainImage)
                    .Include(l => l.Images.Where(img => !img.IsDeleted))
                    .AsQueryable();

                if (filter != null) query = query.Where(filter);

                var totalCount = await query.CountAsync(ct);

                var items = await query
                    .OrderByDescending(l => l.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .AsNoTracking()
                    .ToListAsync(ct);

                return (Listings: items,TotalCount: totalCount);
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching host view listings in repository.", ex);
            }
        }

        /// Admin view with optional includeDeleted flag.
        public async Task<(IEnumerable<Listing> Listings, int TotalCount)> GetAdminViewAsync(
            Expression<Func<Listing, bool>>? filter = null,
            int page = 1,
            int pageSize = 10,
            bool includeDeleted = false,
            CancellationToken ct = default)
        {
            try
            {
                var query = _context.Listings.AsQueryable();

                if (!includeDeleted)
                    query = query.Where(l => !l.IsDeleted);

                query = query
                    .Include(l => l.MainImage)
                    .Include(l => l.Images.Where(img => !img.IsDeleted))
                    .Include(l => l.User);

                if (filter != null) query = query.Where(filter);

                var totalCount = await query.CountAsync(ct);

                var items = await query
                    .OrderByDescending(l => l.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .AsNoTracking()
                    .ToListAsync(ct);

                return (items, totalCount);
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching admin view listings in repository.", ex);
            }
        }

        /// Listings awaiting review (IsReviewed == false).
        public async Task<(IEnumerable<Listing> Listings, int TotalCount)> GetPendingApprovalsAsync(
            Expression<Func<Listing, bool>>? filter = null,
            int page = 1,
            int pageSize = 10,
            CancellationToken ct = default)
        {
            try
            {
                var query = _context.Listings
                    .Where(l => !l.IsDeleted && !l.IsReviewed)
                    .Include(l => l.MainImage)
                    .Include(l => l.Images.Where(img => !img.IsDeleted))
                    .Include(l => l.User)
                    .AsQueryable();

                if (filter != null) query = query.Where(filter);

                var totalCount = await query.CountAsync(ct);

                var items = await query
                    .OrderBy(l => l.SubmittedForReviewAt ?? l.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .AsNoTracking()
                    .ToListAsync(ct);

                return (items, totalCount);
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching pending approvals in repository.", ex);
            }
        }

        /// Admin approves a listing (sets Reviewed/Approved and audit fields).
        public async Task<bool> ApproveAsync(int id, Guid approverUserId, CancellationToken ct = default)
        {
            try
            {
                var listing = await _context.Listings.IgnoreQueryFilters().FirstOrDefaultAsync(l => l.Id == id, ct);
                if (listing == null) return false;

                var approver = await GetFullNameAsync(approverUserId, ct);
                listing.Approve(approver);

                await _context.SaveChangesAsync(ct);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Error approving listing in repository.", ex);
            }
        }

        /// Admin rejects a listing (sets Reviewed=false and audit fields).
        public async Task<bool> RejectAsync(int id, Guid approverUserId, string? note = null, CancellationToken ct = default)
        {
            try
            {
                var listing = await _context.Listings.IgnoreQueryFilters().FirstOrDefaultAsync(l => l.Id == id, ct);
                if (listing == null) return false;

                var approver = await GetFullNameAsync(approverUserId, ct);
                listing.Reject(approver, note);

                await _context.SaveChangesAsync(ct);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Error rejecting listing in repository.", ex);
            }
        }

        /// Returns true if given userId owns the listing.
        public async Task<bool> IsOwnerAsync(int listingId, Guid userId, CancellationToken ct = default)
        {
            try
            {
                return await _context.Listings.AsNoTracking().AnyAsync(l => l.Id == listingId && l.UserId == userId, ct);
            }
            catch (Exception ex)
            {
                throw new Exception("Error checking listing ownership in repository.", ex);
            }
        }

        /// Get listing by id (includes images/user, excludes deleted).
        public async Task<Listing?> GetListingByIdAsync(int id, CancellationToken ct = default)
        {
            try
            {
                return await _context.Listings
                    .Include(l => l.Images.Where(img => !img.IsDeleted))
                    .Include(l => l.MainImage)
                    .Include(l => l.User)
                    .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted, ct);
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching listing by id in repository.", ex);
            }
        }

        /// Promote listing: set promoted flag and end date.
        public async Task<bool> PromoteAsync(int id, DateTime promotionEndDate, Guid performedByUserId, CancellationToken ct = default)
        {
            try
            {
                var listing = await _context.Listings.IgnoreQueryFilters().FirstOrDefaultAsync(l => l.Id == id, ct);
                if (listing == null) return false;

                var performer = await GetFullNameAsync(performedByUserId, ct);
                listing.SetPromotion(true, promotionEndDate, performer);

                await _context.SaveChangesAsync(ct);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Error promoting listing in repository.", ex);
            }
        }

        /// Set main image for listing (caller should ensure permission). Uses domain SetMainImage.
        public async Task<bool> SetMainImageAsync(int listingId, int imageId, string performedBy, CancellationToken ct = default)
        {
            try
            {
                var listing = await _context.Listings.Include(l => l.Images).FirstOrDefaultAsync(l => l.Id == listingId && !l.IsDeleted, ct);
                if (listing == null) return false;

                var img = listing.Images.FirstOrDefault(i => i.Id == imageId && !i.IsDeleted);
                if (img == null) return false;

                listing.SetMainImage(img, performedBy);

                await _context.SaveChangesAsync(ct);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Error setting main image in repository.", ex);
            }
        }
    }
}
