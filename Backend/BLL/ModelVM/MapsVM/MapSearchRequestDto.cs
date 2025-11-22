using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.ModelVM.MapsVM
{
    public class MapSearchRequestDto
    {
        public double NorthEastLat { get; set; }
        public double NorthEastLng { get; set; }
        public double SouthWestLat { get; set; }
        public double SouthWestLng { get; set; }

        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? MinBedrooms { get; set; }
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
    }
}
