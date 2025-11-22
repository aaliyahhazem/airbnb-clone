using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.ModelVM.MapsVM
{
    public class MapSearchResponseDto
    {
        public List<PropertyMapDto> Properties { get; set; } = new();
        public int TotalCount { get; set; }
    }
}
