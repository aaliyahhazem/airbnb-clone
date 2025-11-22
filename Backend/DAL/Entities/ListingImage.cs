namespace DAL.Entities
{
    public class ListingImage
    {
        public int Id { get; private set; }
        public string ImageUrl { get; private set; } = null!;

        // Relationships
        public int ListingId { get; private set; }
        public Listing Listing { get; private set; } = null!;

        // Auditing & soft-delete
        public DateTime CreatedAt { get; private set; }
        public string CreatedBy { get; private set; } = null!;
        public string? UpdatedBy { get; private set; }
        public DateTime? UpdatedOn { get; private set; }
        public string? DeletedBy { get; private set; }
        public DateTime? DeletedOn { get; private set; }
        public bool IsDeleted { get; private set; }


        // Private constructor for EF
        protected ListingImage() { }

        // Factory method
        public static ListingImage CreateImage(Listing listing, string imageUrl, string createdBy)
        {
            if (listing == null) throw new ArgumentNullException(nameof(listing));
            if (string.IsNullOrWhiteSpace(imageUrl)) throw new ArgumentException("image url must be provided", nameof(imageUrl));

            var li = new ListingImage
            {
                Listing = listing,                 // navigation set
                                                   // DO NOT set ListingId here
                ImageUrl = imageUrl,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            return li;
        }

        // Update image
        public bool UpdateImage(string imageUrl, string updatedBy)
        {
            if (IsDeleted)
                return false;


            var changed = false;
            if (ImageUrl != imageUrl)
            {
                ImageUrl = imageUrl;
                changed = true;
            }

            if (changed)
            {
                UpdatedBy = updatedBy;
                UpdatedOn = DateTime.UtcNow;
            }

            return changed;
        }

        // Soft delete
        public bool SoftDelete(string deletedBy)
        {
            if (IsDeleted)
                return false;

            IsDeleted = true;
            DeletedBy = deletedBy;
            DeletedOn = DateTime.UtcNow;
            return true;
        }

        //hard delete 
    }
}