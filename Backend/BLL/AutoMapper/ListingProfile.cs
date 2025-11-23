



//namespace BLL.AutoMapper
//{
//    public class ListingProfile : Profile
//    {
//        public ListingProfile()
//        {
//            CreateMap<ListingImage, ListingImage>().ReverseMap();

//            CreateMap<Listing, ListingVM>()
//                .ForMember(d => d.Images, opt => opt.MapFrom(s => s.Images))
//                .ForMember(d => d.Tags, opt => opt.MapFrom(s => s.Tags ?? new List<string>()))
//                .ForMember(d => d.CreatedBy, opt => opt.MapFrom(s => s.CreatedBy))
//                .ForMember(d => d.IsApproved, opt => opt.MapFrom(s => s.IsApproved))
//                .ForMember(d => d.IsReviewed, opt => opt.MapFrom(s => s.IsReviewed));
//        }
//    }
//}
