using BLL.ModelVM.Favorite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.AutoMapper
{
    public class FavoriteProfile : Profile
    {
        public FavoriteProfile()
        {
            // Favorite → FavoriteVM
            CreateMap<Favorite, FavoriteVM>()
                .ForMember(d => d.Listing, opt => opt.MapFrom(s => s.Listing));

            // Listing → FavoriteListingVM
            CreateMap<Listing, FavoriteListingVM>()
                .ForMember(d => d.MainImageUrl,
                    opt => opt.MapFrom(s => s.MainImage != null ? s.MainImage.ImageUrl : null))
                .ForMember(d => d.FavoriteCount,
                    opt => opt.Ignore()); // Set manually in service
        }
    }

}