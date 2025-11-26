using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.ModelVM.Favorite
{
    public class TopFavoritedListingVM
    {
        public int ListingId { get; set; }
        public string Title { get; set; } = null!;
        public int FavoriteCount { get; set; }
    }
}
