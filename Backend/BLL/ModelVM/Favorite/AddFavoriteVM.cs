using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.ModelVM.Favorite
{
    public class AddFavoriteVM
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Listing ID must be greater than 0")]
        public int ListingId { get; set; }
    }
}
