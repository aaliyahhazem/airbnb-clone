using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities
{
    public class Favorite
    {
        public int Id { get; private set; }
        public Guid UserId { get; private set; }
        public int ListingId { get; private set; }
        public DateTime CreatedAt { get; private set; }
        // Navigation properties
        public User User { get; private set; } = null!;
        public Listing Listing { get; private set; } = null!;

        private Favorite() { }
        public static Favorite Create(Guid userId, int listingId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty", nameof(userId));

            if (listingId <= 0)
                throw new ArgumentException("Listing ID must be greater than zero", nameof(listingId));

            return new Favorite
            {
                UserId = userId,
                ListingId = listingId,
                CreatedAt = DateTime.UtcNow
            };
        }
        public bool BelongsToUser(Guid userId)
        {
            return UserId == userId;
        }
    }
}
