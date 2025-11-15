
namespace DAL.Entities
{
    public class ListingImage
    {
        public int Id { get; private set; }
        public string ImageUrl { get; private set; } = null!;

        // Relationships
        public int ListingId { get; private set; }
        public Listing Listing { get; private set; } = null!;

        #region Auditing & soft-delete
        public DateTime CreatedAt { get; private set; }
        public string CreatedBy { get; private set; } = null!;
        public string? UpdatedBy { get; private set; }
        public DateTime? UpdatedOn { get; private set; }

        public string? DeletedBy { get; private set; }
        public DateTime? DeletedOn { get; private set; }

        public bool IsDeleted { get; private set; }
        #endregion

        //  concurrency token
        public byte[]? RowVersion { get; private set; }

        protected ListingImage() { }

        // Create a new listing image
        public static ListingImage Create(int listingId, string imageUrl, string? createdBy = null)
        {
            return new ListingImage
            {
                ListingId = listingId,
                ImageUrl = imageUrl,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy ?? "System",
                IsDeleted = false
            };
        }

        // Update existing image
        public bool Update(string imageUrl, string? updatedBy)
        {
            if (IsDeleted) return false;

            var changed = false;

            if (!string.IsNullOrWhiteSpace(imageUrl) && ImageUrl != imageUrl)
            {
                ImageUrl = imageUrl;
                changed = true;
            }

            if (changed)
            {
                UpdatedBy = updatedBy ?? "System";
                UpdatedOn = DateTime.UtcNow;
            }

            return changed;
        }

        // Soft delete image
        public void SoftDelete(string? deletedBy)
        {
            if (IsDeleted) return;
            IsDeleted = true;
            DeletedOn = DateTime.UtcNow;
            DeletedBy = deletedBy ?? "System";
        }
    }
}
