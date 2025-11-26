using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.ModelVM.Favorite
{
    public class FavoriteStatsVM
    {
        public int TotalFavorites { get; set; }
        public int UniqueUsers { get; set; }
        public int UniqueListings { get; set; }
        public List<TopFavoritedListingVM> TopListings { get; set; } = new();
    }
}
