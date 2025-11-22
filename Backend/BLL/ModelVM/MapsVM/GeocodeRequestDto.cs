using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.ModelVM.MapsVM
{
    public class GeocodeRequestDto
    {
        public string Address { get; set; } = string.Empty;
        public string? Country { get; set; }
        public string? City { get; set; }
        public string? Street { get; set; }


    }
}
