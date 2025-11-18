namespace DAL.Repo.Implementation
{
    public class ListingImageRepository : GenericRepository<ListingImage>, IListingImageRepository
    {
        private readonly AppDbContext _context;

        public ListingImageRepository(AppDbContext context) : base(context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// Return main image for listing (explicit MainImage if set, otherwise null).
        public async Task<ListingImage?> GetMainImageByListingIdAsync(int listingId, CancellationToken ct = default)
        {
            try
            {
                var listing = await _context.Listings
                    .Include(l => l.MainImage)
                    .FirstOrDefaultAsync(l => l.Id == listingId && !l.IsDeleted, ct);

                return listing?.MainImage;
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching main image for listing in repository.", ex);
            }
        }

        /// Get all images for a listing (including deleted).
        public async Task<IEnumerable<ListingImage>> GetImagesByListingIdAsync(int listingId, CancellationToken ct = default)
        {
            try
            {
                return await _context.ListingImages
                    .Where(img => img.ListingId == listingId)
                    .Include(img => img.Listing)
                    .OrderBy(img => img.CreatedAt)
                    .AsNoTracking()
                    .ToListAsync(ct);
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching images for listing in repository.", ex);
            }
        }

        /// Get active (non-deleted) images for a listing.
        public async Task<IEnumerable<ListingImage>> GetActiveImagesByListingIdAsync(int listingId, CancellationToken ct = default)
        {
            try
            {
                return await _context.ListingImages
                    .Where(img => img.ListingId == listingId && !img.IsDeleted)
                    .OrderBy(img => img.CreatedAt)
                    .AsNoTracking()
                    .ToListAsync(ct);
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching active images for listing in repository.", ex);
            }
        }

        /// Get image by id (only non-deleted).
        public async Task<ListingImage?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            try
            {
                return await _context.ListingImages
                    .Include(img => img.Listing)
                    .FirstOrDefaultAsync(img => img.Id == id && !img.IsDeleted, ct);
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching listing image by id in repository.", ex);
            }
        }

        /// Add multiple images to a listing. Returns number of images added.
        public async Task<int> AddImagesAsync(int listingId, List<string> imageUrls, string createdBy, CancellationToken ct = default)
        {
            try
            {
                var listing = await _context.Listings.FirstOrDefaultAsync(l => l.Id == listingId && !l.IsDeleted, ct);
                if (listing == null) return 0;

                var images = new List<ListingImage>();
                foreach (var imageUrl in imageUrls)
                {
                    if (!string.IsNullOrWhiteSpace(imageUrl))
                    {
                        var image = ListingImage.CreateImage(listing, imageUrl, createdBy);
                        images.Add(image);
                    }
                }

                if (!images.Any()) return 0;

                await _context.ListingImages.AddRangeAsync(images, ct);
                await _context.SaveChangesAsync(ct);
                return images.Count;
            }
            catch (Exception ex)
            {
                throw new Exception("Error adding listing images in repository.", ex);
            }
        }

        /// Soft-delete a batch of images.
        public async Task<bool> SoftDeleteImagesAsync(List<int> imageIds, string deletedBy, CancellationToken ct = default)
        {
            try
            {
                var images = await _context.ListingImages
                    .Where(img => imageIds.Contains(img.Id) && !img.IsDeleted)
                    .ToListAsync(ct);

                if (!images.Any()) return false;

                foreach (var image in images)
                {
                    image.SoftDelete(deletedBy);
                }

                await _context.SaveChangesAsync(ct);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Error soft deleting listing images in repository.", ex);
            }
        }

        /// Set main image for listing. Uses domain SetMainImage.
        public async Task<bool> SetMainImageAsync(int listingId, int imageId, string performedBy, CancellationToken ct = default)
        {
            try
            {
                var listing = await _context.Listings
                    .Include(l => l.Images)
                    .FirstOrDefaultAsync(l => l.Id == listingId && !l.IsDeleted, ct);

                if (listing == null) return false;

                var image = listing.Images.FirstOrDefault(i => i.Id == imageId && !i.IsDeleted);
                if (image == null) return false;

                listing.SetMainImage(image, performedBy);

                await _context.SaveChangesAsync(ct);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Error setting main image in repository.", ex);
            }
        }

        /// Check if image exists and belongs to listing (non-deleted).
        public async Task<bool> ImageExistsAsync(int imageId, int listingId, CancellationToken ct = default)
        {
            try
            {
                return await _context.ListingImages
                    .AnyAsync(img => img.Id == imageId && img.ListingId == listingId && !img.IsDeleted, ct);
            }
            catch (Exception ex)
            {
                throw new Exception("Error checking image existence in repository.", ex);
            }
        }

        /// Check whether the listing that owns the image belongs to userId.
        public async Task<bool> IsImageOwnerAsync(int imageId, Guid userId, CancellationToken ct = default)
        {
            try
            {
                return await _context.ListingImages
                    .Include(img => img.Listing)
                    .AnyAsync(img => img.Id == imageId && img.Listing.UserId == userId && !img.IsDeleted, ct);
            }
            catch (Exception ex)
            {
                throw new Exception("Error checking image ownership in repository.", ex);
            }
        }

        /// Paged images query (non-deleted) with optional filter.
        public async Task<(IEnumerable<ListingImage> Images, int TotalCount)> GetImagesAsync(
            Expression<Func<ListingImage, bool>>? filter = null,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default)
        {
            try
            {
                var query = _context.ListingImages
                    .Where(img => !img.IsDeleted)
                    .Include(img => img.Listing)
                    .AsQueryable();

                if (filter != null) query = query.Where(filter);

                var totalCount = await query.CountAsync(ct);

                var items = await query
                    .OrderBy(img => img.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .AsNoTracking()
                    .ToListAsync(ct);

                return (items, totalCount);
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching images page in repository.", ex);
            }
        }

        /// Get image by id (non-deleted).
        public async Task<ListingImage?> GetImageByIdAsync(int id, CancellationToken ct = default)
        {
            try
            {
                return await _context.ListingImages
                    .Include(img => img.Listing)
                    .FirstOrDefaultAsync(img => img.Id == id && !img.IsDeleted, ct);
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching listing image by id in repository.", ex);
            }
        }
    }
}
