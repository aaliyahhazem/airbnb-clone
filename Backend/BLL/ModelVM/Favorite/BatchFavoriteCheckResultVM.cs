using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.ModelVM.Favorite
{
    public class BatchFavoriteCheckResultVM
    {
        public Dictionary<int, bool> Results { get; set; } = new();

    }
}
