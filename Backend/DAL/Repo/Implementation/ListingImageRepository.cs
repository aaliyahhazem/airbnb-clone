
namespace DAL.Repo.Implementation
{
    public class ListingImageRepository : GenericRepository<ListingImage>, IListingImageRepository
    {
        private new readonly AppDbContext _context;

        public ListingImageRepository(AppDbContext context) : base(context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

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

        public async Task<bool> HardDeleteImageById(int imageId, string deletedBy, CancellationToken ct = default)
        {
            try
            {
                var image = await _context.ListingImages
                    .FirstOrDefaultAsync(img => img.Id == imageId, ct);

                if (image == null)
                    return false;

                _context.ListingImages.Remove(image);
                await _context.SaveChangesAsync(ct);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Error hard deleting listing image in repository.", ex);
            }
        }
    }
}
