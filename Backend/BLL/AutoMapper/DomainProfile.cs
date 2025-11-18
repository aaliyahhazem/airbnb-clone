namespace BLL.AutoMapper
{
    public class DomainProfile : Profile
    {
        public DomainProfile() 
        {
            // notifications
            CreateMap<Notification, GetNotificationVM>().ReverseMap();
            CreateMap<Notification, CreateNotificationVM>().ReverseMap();


            // Listing -> OverviewVM
            CreateMap<Listing, ListingOverviewVM>()
             .ForMember(d => d.MainImageUrl, opt => opt.MapFrom(s => s.MainImage != null ? s.MainImage.ImageUrl : s.Images.OrderBy(i => i.Id).FirstOrDefault().ImageUrl));

            // Listing -> DetailVM
            CreateMap<Listing, ListingDetailVM>()
             .ForMember(d => d.Images, opt => opt.MapFrom(s => s.Images.Where(i => !i.IsDeleted).OrderBy(i => i.CreatedAt)))
             .ForMember(d => d.MainImageUrl, opt => opt.MapFrom(s => s.MainImage != null ? s.MainImage.ImageUrl : s.Images.Where(i => !i.IsDeleted).OrderBy(i => i.Id).Select(i => i.ImageUrl).FirstOrDefault()));

            // ListingImage -> ListingImageVM
            CreateMap<ListingImage, ListingImageVM>();

            // ListingDetailVM -> ListingUpdateVM (or direct from Listing -> ListingUpdateVM)
            CreateMap<ListingDetailVM, ListingUpdateVM>()
                .ForMember(d => d.Tags, o => o.MapFrom(s => s.Tags ?? new List<string>()))
                // NewImages field should be null; we're only returning existing images to display
                .ForMember(d => d.NewImages, o => o.Ignore())
                .ForMember(d => d.RemoveImageIds, o => o.Ignore());
        }

    }
}
