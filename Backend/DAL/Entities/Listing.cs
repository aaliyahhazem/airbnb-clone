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
        public ICollection<ListingImage> Images { get; private set; } = new List<ListingImage>();
        public ICollection<Favorite> Favorites { get; private set; } = new List<Favorite>();

        // Main Image
        public int? MainImageId { get; private set; }
        public ListingImage? MainImage { get; private set; }

        //public List<string> Tags { get; private set; } = new List<string>();

        // Approval Workflow
        public bool IsReviewed { get; private set; }
        public bool IsApproved { get; private set; }
        public DateTime? SubmittedForReviewAt { get; private set; }
        public DateTime? ReviewedAt { get; private set; }
        public string? ReviewNotes { get; private set; }
        public string? ReviewedBy { get; private set; }

        // Auditing
        public string CreatedBy { get; private set; } = null!;
        public string? UpdatedBy { get; private set; }
        public DateTime? UpdatedOn { get; private set; }
        public string? DeletedBy { get; private set; }
        public DateTime? DeletedOn { get; private set; }
        public bool IsDeleted { get; private set; }

        //additional property
        public string Destination { get; set; } 
        public string Type { get; set; }

        public int NumberOfRooms { get; set; }        //#of rooms
        public int NumberOfBathrooms { get; set; }   //#of bathrooms

        // Private constructor for EF
        private Listing() { }

        // Factory method for creating new listing
        public static Listing Create(
     string title,
     string description,
     decimal pricePerNight,
     string location,
     double latitude,
     double longitude,
     int maxGuests,
     Guid userId,
     string createdBy,
     string mainImageUrl,
     string destination,
     string type,
        int numberOfRooms,
        int numberOfBathrooms,
     //bool isPromoted = false,
     //DateTime? promotionEndDate = null,

     List<string>? keywordNames = null)     // optional keyword names
        {
            var listing = new Listing
            {
                Title = title,
                Description = description,
                PricePerNight = pricePerNight,
                Location = location,
                Latitude = latitude,
                Longitude = longitude,
                MaxGuests = maxGuests,
                UserId = userId,
                Destination = destination,
                Type = type,
                NumberOfRooms = numberOfRooms,
                NumberOfBathrooms = numberOfBathrooms,
                //IsPromoted = isPromoted,
                //PromotionEndDate = promotionEndDate,

                // Approval workflow - new listings need review
                IsReviewed = false,
                IsApproved = false,
                SubmittedForReviewAt = DateTime.UtcNow,

                // Auditing
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            // Attach keywords owned by this listing
            if (keywordNames != null)
            {
                foreach (var name in keywordNames
                             .Where(n => !string.IsNullOrWhiteSpace(n))
                             .Select(n => n.Trim()))
                {
                    var kw = Amenity.Create(name, listing);
                    listing.Amenities.Add(kw);
                }
            }

            if (!string.IsNullOrWhiteSpace(mainImageUrl))
            {
                var mainImage = ListingImage.CreateImage(listing, mainImageUrl, createdBy);
                listing.Images.Add(mainImage);
                // no SetMainImage call here
            }

            return listing;
        }

        internal void AddImage(string imageUrl, string createdBy)
        {
            if (IsDeleted)
                throw new InvalidOperationException("Cannot add image to deleted listing");

            var image = ListingImage.CreateImage(this, imageUrl, createdBy);
            Images.Add(image);
        }

        internal bool Update(
    string title,
    string description,
    decimal pricePerNight,
    string location,
    double latitude,
    double longitude,
    int maxGuests,
    string updatedBy,
     string destination,
 string type,
    int numberOfRooms,
    int numberOfBathrooms,
    //bool isPromoted,
    //DateTime? promotionEndDate,
    IEnumerable<string>? keywordNames = null,
    string? newMainImageUrl = null)
        {
            if (IsDeleted)
                return false;

            Title = title;
            Description = description;
            PricePerNight = pricePerNight;
            Location = location;
            Latitude = latitude;
            Longitude = longitude;
            MaxGuests = maxGuests;
            Destination = destination;
            Type = type;
            NumberOfRooms = numberOfRooms;
            NumberOfBathrooms = numberOfBathrooms;
            //IsPromoted = isPromoted;
            //PromotionEndDate = promotionEndDate;

            // Replace keywords: easiest approach — clear then add new owned keywords
            if (keywordNames != null)
            {
                // Remove existing keywords (they will be deleted on SaveChanges if tracked)
                // We clear the collection so EF removes them from the database on SaveChanges (cascade)
                Amenities.Clear();

                foreach (var name in keywordNames
                             .Where(n => !string.IsNullOrWhiteSpace(n))
                             .Select(n => n.Trim()))
                {
                    var kw = Amenity.Create(name, this);
                    Amenities.Add(kw);
                }
            }

            if (!string.IsNullOrWhiteSpace(newMainImageUrl))
            {
                var newMainImage = ListingImage.CreateImage(this, newMainImageUrl, updatedBy);
                Images.Add(newMainImage);
                SetMainImage(newMainImage, updatedBy);
            }

            UpdatedBy = updatedBy;
            UpdatedOn = DateTime.UtcNow;

            if (IsReviewed)
            {
                MarkForReReview();
            }

            return true;
        }
        // Mark for re-review when host makes changes
        internal void MarkForReReview()
        {
            if (IsDeleted) return;

            IsReviewed = false;
            IsApproved = false;
            SubmittedForReviewAt = DateTime.UtcNow;
            ReviewNotes = null;
            ReviewedBy = null;
            ReviewedAt = null;
        }

        // Admin approval
        internal void Approve(string approver, string? notes = null)
        {
            if (IsDeleted)
                throw new InvalidOperationException("Cannot approve a deleted listing.");
            if (IsReviewed && IsApproved)
                throw new InvalidOperationException("Listing is already approved.");

            IsReviewed = true;
            IsApproved = true;
            ReviewedBy = approver;
            ReviewedAt = DateTime.UtcNow;
            ReviewNotes = notes;
            UpdatedBy = approver;
            UpdatedOn = DateTime.UtcNow;
        }

        // Admin rejection
        internal void Reject(string rejectedBy, string? notes = null)
        {
            if (IsDeleted)
                throw new InvalidOperationException("Cannot reject a deleted listing.");
            if(!IsApproved)
                throw new InvalidOperationException("listing is already rejected.");


            IsReviewed = true;
            IsApproved = false;
            ReviewedBy = rejectedBy;
            ReviewedAt = DateTime.UtcNow;
            ReviewNotes = notes;
            UpdatedBy = rejectedBy;
            UpdatedOn = DateTime.UtcNow;
        }

        // Soft delete
        internal bool SoftDelete(string deletedBy)
        {
            if (IsDeleted)
                throw new InvalidOperationException("listing is already deleted.");

            IsDeleted = true;
            DeletedBy = deletedBy;
            DeletedOn = DateTime.UtcNow;
            return true;
        }

        // Set main image
        public void SetMainImage(ListingImage image, string performedBy)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (IsDeleted) throw new InvalidOperationException("Cannot change main image...");

            // Ensure the image belongs to this listing and is not deleted
            if (!ReferenceEquals(image.Listing, this) && image.ListingId != this.Id)
                throw new InvalidOperationException("Image does not belong to this listing.");

            if (image.IsDeleted)
                throw new InvalidOperationException("Cannot set a deleted image as main.");

            // If image.Id is 0 (not persisted yet) that's OK — EF will set the PK when saved.
            MainImageId = image.Id;
            MainImage = image;
            UpdatedBy = performedBy;
            UpdatedOn = DateTime.UtcNow;
        }

        // Sets or updates the promotion status of a listing.
        // Promoted listings appear at the top of search results.
        // Validates that promotion end date is in the future.
        // Prevents promoting already promoted listings.
        internal void SetPromotion(bool isPromoted, DateTime? promotionEndDate, string performedBy)
        {
            if (IsDeleted)
                throw new InvalidOperationException("Cannot promote a deleted listing.");

            //  Check if already promoted
            if (IsPromoted && isPromoted)
                throw new InvalidOperationException($"Listing is already promoted until {PromotionEndDate?.ToString("yyyy-MM-dd HH:mm")}.");

            // Validate promotion end date is in the future
            if (isPromoted && promotionEndDate.HasValue)
            {
                if (promotionEndDate.Value <= DateTime.UtcNow)
                    throw new InvalidOperationException("Promotion end date must be in the future.");
            }

            //  If unpromoting, clear the end date
            if (!isPromoted)
            {
                IsPromoted = false;
                PromotionEndDate = null;
            }
            else
            {
                IsPromoted = true;
                PromotionEndDate = promotionEndDate;
            }

            UpdatedBy = performedBy;
            UpdatedOn = DateTime.UtcNow;
        }
    
        // Checks if the promotion has expired and automatically unpromotes if needed.
        // Should be called when retrieving listings for public display
        public bool CheckAndExpirePromotion()
        {
            if (IsPromoted && PromotionEndDate.HasValue && PromotionEndDate.Value <= DateTime.UtcNow)
            {
                IsPromoted = false;
                PromotionEndDate = null;
                return true;
            }
            return false;
        }

        // Extends the current promotion to a new end date.
        // Validates that listing is currently promoted and new date is valid.
        // Can only be called by Admin.
        internal void ExtendPromotion(DateTime newPromotionEndDate, string performedBy)
        {
            if (IsDeleted)
                throw new InvalidOperationException("Cannot extend promotion of a deleted listing.");

            if (!IsPromoted || !PromotionEndDate.HasValue)
                throw new InvalidOperationException("Listing is not currently promoted.");

            if (newPromotionEndDate <= PromotionEndDate.Value)
                throw new InvalidOperationException("New promotion end date must be after the current end date.");

            if (newPromotionEndDate <= DateTime.UtcNow)
                throw new InvalidOperationException("New promotion end date must be in the future.");

            PromotionEndDate = newPromotionEndDate;
            UpdatedBy = performedBy;
            UpdatedOn = DateTime.UtcNow;
        }
    

// Set main image for seedings
public void SetMainImage(int imageId, string performedBy) 
        { if (IsDeleted)
                throw new InvalidOperationException("Cannot change main image..."); 
            if (!Images.Any(img => img.Id == imageId && !img.IsDeleted)) 
                throw new InvalidOperationException("Image not found in listing"); 
            MainImageId = imageId;
            UpdatedBy = performedBy;
            UpdatedOn = DateTime.UtcNow;
        }


    }
}