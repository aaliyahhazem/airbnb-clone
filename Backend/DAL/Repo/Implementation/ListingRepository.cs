using System.Linq.Expressions;

namespace DAL.Repo.Implementation
{
    public class ListingRepository : GenericRepository<Listing>, IListingRepository
    {
        public ListingRepository(AppDbContext context) : base(context) { }

        private async Task<string?> GetHostFullNameAsync(Guid hostUserId)
        {
            return await _context.Users
                .Where(u => u.Id == hostUserId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync();
        }


       
        // Fix for the method `GetPagedOverviewAsync` to resolve CS0266 error
        public async Task<IEnumerable<Listing>> GetPagedOverviewAsync(int page, int pageSize, Expression<Func<Listing, bool>>? filter = null, CancellationToken ct = default)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;

                IQueryable<Listing> query; // Declare query as IQueryable<Listing>

                if (filter != null)
                {
                    query = _context.Listings.Where(filter)
                        .OrderByDescending(l => l.IsPromoted)
                        .ThenByDescending(l => l.CreatedAt)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .Include(l => l.Images.Where(img => img.Id == l.MainImageId)) // Include only MainImage
                        .AsNoTracking();
                }
                else
                {
                    query = _context.Listings
                        .OrderByDescending(l => l.IsPromoted)
                        .ThenByDescending(l => l.CreatedAt)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .Include(l => l.Images.Where(img => img.Id == l.MainImageId)) // Include only MainImage
                        .AsNoTracking();
                }

                return await query.ToListAsync(ct);
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching paged overview listings.", ex);
            }
        }

        // Details: returns listing with full images collection in details view
        public async Task<Listing?> GetByIdWithImagesAsync(int id, CancellationToken ct = default)
        {
            try
            {
                return await _context.Listings
                    .Where(l => l.Id == id)
                    .Include(l => l.Images)
                    .Include(l => l.MainImage)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ct);

            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching listing by id with images.", ex);
            }
        }

        // General fetch (no includes) without images
        public async Task<Listing?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            try
            {
                return await _context.Listings
                    .Where(l => l.Id == id)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ct);

            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching listing by id.", ex);
            }
        }

        // Check existence by id
        public async Task<bool> ExistsAsync(int id, CancellationToken ct = default)
        {
            try
            {
                return await _context.Listings
                    .AsNoTracking()
                    .AnyAsync(l => l.Id == id, ct);

            }
            catch (Exception ex)
            {
                throw new Exception("Error checking existence of listing.", ex);
            }
        }

        public async Task<int> AddAsync(Listing listing, Guid hostId, CancellationToken ct = default)
        {
            try
            {
                if (listing == null) throw new ArgumentNullException(nameof(listing));
                if (hostId == Guid.Empty) throw new ArgumentException(nameof(hostId));

                var hostFullName = await GetHostFullNameAsync(hostId)
                                   ?? throw new InvalidOperationException("Host not found.");
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
                isPromoted: listing.IsPromoted,
                promotionEndDate: listing.PromotionEndDate
                );
                await _context.Listings.AddAsync(newListing, ct);
                await _context.SaveChangesAsync(ct);
                return newListing.Id;
            }


            catch (Exception ex)
            {
                throw new Exception("Error adding listing.", ex);
            }
        }

        public async Task<bool> UpdateAsync(int listingId, Guid hostId, Listing listing, CancellationToken ct = default)
        {
            try
            {
                if (listing == null)
                    throw new ArgumentNullException(nameof(listing));

                var hostFullName = await GetHostFullNameAsync(hostId)
                                    ?? throw new InvalidOperationException("Host not found.");

                var entity = await _context.Listings.IgnoreQueryFilters().FirstOrDefaultAsync(l => l.Id == listingId);// allow editing even if currently pending/hidden
                if (entity == null)
                {
                    return false;
                }
                if (entity.IsDeleted)
                {
                    throw new InvalidOperationException("Cannot update a deleted listing.");
                }

                if (entity.UserId != hostId)
                {
                    throw new UnauthorizedAccessException("You are not authorized to update this listing.");
                }
                // Update fields
                var updated = entity.Update(
                    listing.Title,
                    listing.Description,
                    listing.PricePerNight,
                    listing.Location,
                    listing.Latitude,
                    listing.Longitude,
                    listing.MaxGuests,
                    hostFullName,                  // updatedBy
                    listing.IsPromoted,            // isPromoted
                    listing.PromotionEndDate,      // promotionEndDate
                    listing.Tags                   // tags
                );
                if (!updated)
                {
                    return false;
                }
                await _context.SaveChangesAsync(ct);
                return true;

            }
            catch (Exception ex)
            {
                throw new Exception("Error updating listing.", ex);
            }
        }

        //get paged listings by owner all statuses
        public async Task<IEnumerable<Listing>> GetByUserAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;

                var query = _context.Listings
                    .Where(l => l.UserId == userId && !l.IsDeleted);// exclude deleted listings

                var items = await query
                    .OrderByDescending(l => l.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Include(l => l.MainImage)
                    .AsNoTracking()
                    .ToListAsync(ct);

                return items;

            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching listings by user.", ex);
            }
        }

        public async Task<bool> SoftDeleteByOwnerAsync(int listingId, Guid hostId, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsOwnerAsync(int listingId, Guid userId, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SoftDeleteAsync(int id, Guid performedByUserId, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ApproveAsync(int id, Guid approverUserId, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RejectAsync(int id, Guid approverUserId, string? note = null, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> PromoteAsync(int id, DateTime promotionEndDate, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<Listing?> GetByIdAdminAsync(int id, bool includeDeleted = false, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ApproveListingAsync(int listingId, string approvedBy)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RejectListingAsync(int listingId, string rejectedBy)
        {
            throw new NotImplementedException();
        }
    


    }
}
