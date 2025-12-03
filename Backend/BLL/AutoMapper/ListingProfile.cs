



// UNUSED: This entire file is not used and has been replaced by DomainProfile.cs
// The mapping functionality has been moved to DomainProfile.cs which handles all entity-to-viewmodel mappings
// This file can be safely deleted or kept for reference

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
