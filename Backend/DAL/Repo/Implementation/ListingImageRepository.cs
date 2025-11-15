
namespace DAL.Repo.Implementation
{
    public class ListingImageRepository : GenericRepository<ListingImage>, IListingImageRepository
    {
        public ListingImageRepository(AppDbContext context) : base(context) { }

        public async Task<List<ListingImage>> AddImagesToListAsync(int listingId, string imgPath, string createdBy)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(imgPath)) throw new ArgumentException("imgPath is required.", nameof(imgPath));

                var existListing = await _context.Listings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(l => l.Id == listingId);

                if (existListing == null)
                    throw new InvalidOperationException($"Listing {listingId} does not exist.");

                var listingImage = ListingImage.Create(listingId, imgPath, createdBy);
                await _context.ListingImages.AddAsync(listingImage);
                await _context.SaveChangesAsync();

                return new List<ListingImage> { listingImage };

            }
            catch (Exception ex)
            {
                throw new Exception($"Error adding image to listing: {ex.Message}", ex);

            }
        }

        public async Task<bool> DeleteImagesFromListAsync(int listingId, int listingImageId, string deletedBy)
        {
            try
            {
                var listingImage = await _context.ListingImages
                              .Where(li => li.Id == listingImageId && li.ListingId == listingId && !li.IsDeleted)
                              .FirstOrDefaultAsync();

                if (listingImage == null)
                {
                    return false;
                }

                listingImage.SoftDelete(deletedBy);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting image from listing: {ex.Message}", ex);
            }
        }

        public Task<List<ListingImage>> GetAllImagesByListingIdAsync(int listingId)
        {
            try
            {
                return _context.ListingImages
                    .Where(li => li.ListingId == listingId && !li.IsDeleted)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving images for listing: {ex.Message}", ex);
            }
        }

        public async Task<ListingImage> UpdateImageInListingAsync(int listingId, int listingImageId, string newImageUrl, string updatedBy)
        {
            try
            {
                var listingImage = await _context.ListingImages
                    .Where(li => li.Id == listingImageId && li.ListingId == listingId && !li.IsDeleted)
                    .FirstOrDefaultAsync();

                if (listingImage == null) return null;

                var updated = listingImage.Update(newImageUrl, updatedBy);
                if (!updated)
                {
                    return listingImage;
                }

                await _context.SaveChangesAsync();
                return listingImage;

            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating image in listing: {ex.Message}", ex);
            }
        }
    }
}
