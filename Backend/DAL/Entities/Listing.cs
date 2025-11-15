namespace DAL.Entities
{
    public class Listing
    {
        public int Id { get; private set; }
        public string Title { get; private set; } = null!;
        public string Description { get; private set; } = null!;
        public decimal PricePerNight { get; private set; }
        public string Location { get; private set; } = null!;
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public int MaxGuests { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public bool IsPromoted { get; private set; }
        public DateTime? PromotionEndDate { get; private set; }

        // Foreign Key
        public Guid UserId { get; private set; }
        public User User { get; private set; } = null!;

        // Relationships
        public ICollection<Booking> Bookings { get; private set; } = new List<Booking>();
        public ICollection<Review> Reviews { get; private set; } = new List<Review>();
        public ICollection<Amenity> Amenities { get; private set; } = new List<Amenity>();
        public ICollection<Keyword> Keywords { get; private set; } = new List<Keyword>();
        public ICollection<ListingImage> Images { get; private set; } = new List<ListingImage>();

        #region Abdelrahman Mohamed Hamed 
        // optional explicit main image (nullable)
        public int? MainImageId { get; private set; }
        public ListingImage? MainImage { get; private set; }

        public int Priority { get; private set; }

        public List<string> Tags { get; set; } = new List<string>();

        public bool IsReviewed { get; private set; }
        public bool IsApproved { get; private set; }
        public string CreatedBy { get; private set; } = null!;

        public string? UpdatedBy { get; private set; }
        public DateTime? UpdatedOn { get; private set; }

        public string? DeletedBy { get; private set; }
        public DateTime? DeletedOn { get; private set; }

        public bool IsDeleted { get; private set; }

        //  concurrency token
        public byte[]? RowVersion { get; private set; }
        #endregion

        protected Listing() { }

        // Create a new listing
        public static Listing Create(
            string title,
            string description,
            decimal pricePerNight,
            string location,
            double latitude,
            double longitude,
            int maxGuests,
            List<string> tags,
            Guid userId,
            string createdBy,
            bool isPromoted = false,
            DateTime? promotionEndDate = null
        )
        {
            return new Listing
            {
                Title = title,
                Description = description,
                PricePerNight = pricePerNight,
                Location = location,
                Latitude = latitude,
                Longitude = longitude,
                MaxGuests = maxGuests,
                Tags = tags ?? new List<string>(),
                UserId = userId,
                IsPromoted = isPromoted,
                PromotionEndDate = promotionEndDate,
                // moderation & auditing defaults
                IsReviewed = false,
                IsApproved = false,
                IsDeleted = false,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };
        }

        // Update existing listing
        public bool Update(
            string title,
            string description,
            decimal pricePerNight,
            string location,
            double latitude,
            double longitude,
            int maxGuests,
            string updatedBy,
            bool isPromoted,
            DateTime? promotionEndDate,
            List<string>? tags)
        {
            if (!IsDeleted)
            {
                Title = title;
                Description = description;
                PricePerNight = pricePerNight;
                Location = location;
                Latitude = latitude;
                Longitude = longitude;
                MaxGuests = maxGuests;
                IsPromoted = isPromoted;
                PromotionEndDate = promotionEndDate;
                Tags = tags ?? new List<string>();

                // auditing
                UpdatedBy = updatedBy;
                UpdatedOn = DateTime.UtcNow;

                // moderation: re-review
                IsReviewed = false;
                IsApproved = false;
                return true;
            }

            return false;
        }

        public bool SoftDelete(string deletedBy)
        {
            if (IsDeleted)
                return false;
            IsDeleted = true;
            DeletedBy = deletedBy;
            DeletedOn = DateTime.UtcNow;
            return true;
        }

        public void Approve(string approver, string? note = null)
        {
            if (IsDeleted) throw new InvalidOperationException("Cannot approve a deleted listing.");
            IsReviewed = true;
            IsApproved = true;
            UpdatedBy = approver;
            UpdatedOn = DateTime.UtcNow;
        }

        public void Reject(Guid adminId, string? note = null)
        {
            IsReviewed = true;
            IsApproved = false;
        }

        
        public void SetMainImage(int imageId, string performedBy)
        {
            if (IsDeleted) throw new InvalidOperationException("Cannot change main image of a deleted listing.");
            MainImageId = imageId;
            UpdatedBy = performedBy ?? "System";
            UpdatedOn = DateTime.UtcNow;
        }

        public void SetPromotion(bool isPromoted, DateTime? promotionEndDate, string performedBy)
        {
            if (IsDeleted) throw new InvalidOperationException("Cannot promote a deleted listing.");
            IsPromoted = isPromoted;
            PromotionEndDate = promotionEndDate;
            UpdatedBy = performedBy ?? "System";
            UpdatedOn = DateTime.UtcNow;
        }
    }
}
