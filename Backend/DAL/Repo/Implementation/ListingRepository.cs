
namespace DAL.Repo.Implementation
{
    public class ListingRepository : GenericRepository<Listing>, IListingRepository
    {
        private readonly AppDbContext _context;

        public ListingRepository(AppDbContext context) : base(context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        private async Task<string> GetFullNameAsync(Guid userId, CancellationToken ct = default)
        {
            var name = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(ct);

            return string.IsNullOrWhiteSpace(name) ? userId.ToString() : name;
        }

        public async Task<int> CreateAsync(
            Listing listing,
            string mainImageUrl,
            List<string>? additionalImageUrls,
            List<string>? keywordNames,
            Guid hostId,
            CancellationToken ct = default)
        {
            if (listing == null) throw new ArgumentNullException(nameof(listing));
            if (hostId == Guid.Empty) throw new ArgumentException(nameof(hostId));

            var hostFullName = await GetFullNameAsync(hostId, ct);

            var newListing = Listing.Create(
                title: listing.Title,
                description: listing.Description,
                pricePerNight: listing.PricePerNight,
                location: listing.Location,
                latitude: listing.Latitude,
                longitude: listing.Longitude,
                maxGuests: listing.MaxGuests,
                userId: hostId,
                createdBy: hostFullName,
                mainImageUrl: mainImageUrl,
                keywordNames: keywordNames
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

        public async Task<bool> UpdateAsync(
            int listingId,
            Guid hostId,
            Listing updatedListing,
            string? newMainImageUrl,
            List<string>? newAdditionalImages,
            List<int>? imagesToRemove,
            List<string>? keywordNames,
            CancellationToken ct = default)
        {
            try
            {
                if (hostId == Guid.Empty) throw new ArgumentException(nameof(hostId));

                var hostFullName = await GetFullNameAsync(hostId, ct);

                var entity = await _context.Listings
                    .Include(l => l.Images)
                    .Include(l => l.Amenities)
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

                if (keywordNames != null)
                {
                    entity.Amenities.Clear();
                    foreach (var name in keywordNames.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()))
                    {
                        var kw = Amenity.Create(name, entity);
                        entity.Amenities.Add(kw);
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
                    keywordNames
                );

                if (!updated) return false;

                if (newAdditionalImages?.Any() == true)
                {
                    foreach (var url in newAdditionalImages.Where(u => !string.IsNullOrWhiteSpace(u)))
                        entity.AddImage(url!, hostFullName);
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

        public async Task<bool> DeleteAsync(int listingId, Guid hostId, CancellationToken ct = default)
        {
            try
            {
                var hostFullName = await GetFullNameAsync(hostId, ct);

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

        public async Task<(IEnumerable<Listing> Listings, int TotalCount)> GetUserViewAsync(
            ListingFilterDto? filter = null,
            int page = 1,
            int pageSize = 10,
            CancellationToken ct = default)
        {
            const int MaxPageSize = 100;
            try
            {
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

                var query = _context.Listings
                    .Where(l => !l.IsDeleted && l.IsApproved)
                    .Include(l => l.MainImage)
                    .Include(l => l.Images.Where(img => !img.IsDeleted))
                    .Include(l => l.Amenities)
                    .AsQueryable();

                if (filter != null)
                {
                    if (!string.IsNullOrWhiteSpace(filter.Location))
                    {
                        var loc = filter.Location.Trim();
                        query = query.Where(l => l.Location != null && l.Location.Contains(loc));
                    }

                    if (filter.MinPrice.HasValue)
                    {
                        var min = filter.MinPrice.Value;
                        query = query.Where(l => l.PricePerNight >= min);
                    }

                    if (filter.MaxPrice.HasValue)
                    {
                        var max = filter.MaxPrice.Value;
                        query = query.Where(l => l.PricePerNight <= max);
                    }

                    if (filter.Rooms.HasValue)
                    {
                        var rooms = filter.Rooms.Value;
                        query = query.Where(l => l.MaxGuests == rooms);
                    }

                   

                    if (!string.IsNullOrWhiteSpace(filter.Amenity))
                    {
                        var kw = filter.Amenity.Trim();
                        query = query.Where(l =>
                            (l.Title != null && l.Title.Contains(kw)) ||
                            (l.Description != null && l.Description.Contains(kw)) ||
                            l.Amenities.Any(k => k.Word != null && k.Word.Contains(kw))
                        );
                    }

                    if (!string.IsNullOrWhiteSpace(filter.TitleContains))
                    {
                        var t = filter.TitleContains.Trim();
                        query = query.Where(l => l.Title != null && l.Title.Contains(t));
                    }
                }

                var totalCount = await query.CountAsync(ct);

                var items = await query
                    .OrderByDescending(l => l.IsPromoted)
                    .ThenByDescending(l => l.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .AsNoTracking()
                    .AsSplitQuery()
                    .ToListAsync(ct);

                return (items, totalCount);
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching user view listings in repository.", ex);
            }
        }

        public async Task<(IEnumerable<Listing> Listings, int TotalCount)> GetHostViewAsync(
            Guid hostId,
            Expression<Func<Listing, bool>>? filter = null,
            int page = 1,
            int pageSize = 10,
            CancellationToken ct = default)
        {
            try
            {
                if (page <= 0) page = 1;
                if (pageSize <= 0) pageSize = 10;

                var baseQuery = _context.Listings
                    .Where(l => !l.IsDeleted && l.UserId == hostId);

                if (filter != null) baseQuery = baseQuery.Where(filter);

                var totalCount = await baseQuery.CountAsync(ct);

                if (totalCount == 0)
                    return (Enumerable.Empty<Listing>(), 0);

                var pageQuery = baseQuery
                    .OrderByDescending(l => l.IsPromoted)
                    .ThenByDescending(l => l.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize);

                var items = await pageQuery
                    .AsSplitQuery()
                    .Include(l => l.MainImage)
                    .Include(l => l.Images.Where(img => !img.IsDeleted))
                    .Include(l => l.Amenities)
                    .AsNoTracking()
                    .ToListAsync(ct);

                return (items, totalCount);
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching host view listings in repository.", ex);
            }
        }

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
                    .Include(l => l.User)
                    .Include(l => l.Amenities);

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
                    .Include(l => l.Amenities)
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

        public async Task<Listing?> GetListingByIdAsync(int id, CancellationToken ct = default)
        {
            try
            {
                return await _context.Listings
                    .Include(l => l.Amenities)
                    .Include(l => l.Images.Where(img => !img.IsDeleted))
                    .Include(l => l.MainImage)
                    .Include(l => l.User)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted, ct);
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching listing by id in repository.", ex);
            }
        }

        public async Task<bool> PromoteAsync(int id, DateTime promotionEndDate, Guid performedByUserId, CancellationToken ct = default)
        {
            try
            {
                var listing = await _context.Listings.IgnoreQueryFilters().FirstOrDefaultAsync(l => l.Id == id, ct);
                if (listing == null)
                    throw new InvalidOperationException("Listing not found.");

                if (listing.IsPromoted)
                    throw new InvalidOperationException($"Listing is already promoted until {listing.PromotionEndDate}.");

                if (promotionEndDate < DateTime.UtcNow)
                    throw new InvalidOperationException("Promotion end date must be in the future.");

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

        public async Task<bool> SetMainImageAsync(int listingId, int imageId, string performedBy, CancellationToken ct = default)
        {
            try
            {
                var listing = await _context.Listings
                    .Include(l => l.Images)
                    .FirstOrDefaultAsync(l => l.Id == listingId && !l.IsDeleted, ct);

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
