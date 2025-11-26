using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.ModelVM.Favorite
{
    public class FavoriteVM
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public int ListingId { get; set; }
        public DateTime CreatedAt { get; set; }
        // Listing details
        public FavoriteListingVM? Listing { get; set; }
    }
}
