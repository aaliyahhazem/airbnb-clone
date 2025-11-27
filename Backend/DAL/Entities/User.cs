namespace DAL.Entities
{
    public class User : IdentityUser<Guid>
    {
        public string FullName { get; private set; } = null!;
        public UserRole Role { get; private set; }
        public string? ProfileImg { get; private set; }
        public DateTime DateCreated { get; private set; }
        public string? FirebaseUid { get; private set; }
        public bool IsActive { get; private set; }

        // Relationships
        public ICollection<Listing> Listings { get; private set; } = new List<Listing>();
        public ICollection<Booking> Bookings { get; private set; } = new List<Booking>();
        public ICollection<Review> Reviews { get; private set; } = new List<Review>();
        public ICollection<Message> MessagesSent { get; private set; } = new List<Message>();
        public ICollection<Message> MessagesReceived { get; private set; } = new List<Message>();
        public ICollection<Notification> Notifications { get; private set; } = new List<Notification>();
        public ICollection<Favorite> Favorites { get; private set; } = new List<Favorite>();

        private User() { }

        // CreateImage a user
        public static User Create(
            string fullName,
            UserRole role = UserRole.Guest,
            string? profileImg = null,
            string? firebaseUid = null)
        {
            return new User
            {
                FullName = fullName,
                Role = role,
                ProfileImg = profileImg,
                FirebaseUid = firebaseUid,
                DateCreated = DateTime.UtcNow,
                IsActive = true
            };
        }

        // Update user details
        internal void Update(
            string? fullName = null,
            string? profileImg = null,
            string? firebaseUid = null,
            UserRole? role = null,
            bool? isActive = null)
        {
            if (!string.IsNullOrWhiteSpace(fullName))
                FullName = fullName;

            if (profileImg != null)
                ProfileImg = profileImg;

            if (firebaseUid != null)
                FirebaseUid = firebaseUid;

            if (role.HasValue)
                Role = role.Value;

            if (isActive.HasValue)
                IsActive = isActive.Value;
        }
        internal void SetActive(bool active)
        {
            IsActive = active;
        }
    }
}
