using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.ModelVM.Favorite
{
    public class FavoriteListingVM
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public decimal PricePerNight { get; set; }
        public string Location { get; set; } = null!;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int MaxGuests { get; set; }
        public bool IsPromoted { get; set; }
        public string? MainImageUrl { get; set; }
        public int FavoriteCount { get; set; } // How many users favorited this
    }
}
