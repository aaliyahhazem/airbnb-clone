
using BLL.ModelVM.Message;

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
            .ForMember(d => d.MainImageUrl, opt => opt.MapFrom(s => s.MainImage != null
                       ? s.MainImage.ImageUrl
                       : s.Images.Where(i => !i.IsDeleted).OrderBy(i => i.Id).Select(i => i.ImageUrl).FirstOrDefault()))
                        .ForMember(d => d.Amenities, opt => opt.MapFrom(s => s.Amenities.Select(k => k.Word)));


            /// Listing → ListingDetailVM
            CreateMap<Listing, ListingDetailVM>()
                            .ForMember(d => d.Images, opt => opt.MapFrom(s =>
                                s.Images.Where(i => !i.IsDeleted)
                                        .OrderBy(i => i.CreatedAt)))
                            .ForMember(d => d.MainImageUrl, opt => opt.MapFrom(s =>
                                s.MainImage != null
                                    ? s.MainImage.ImageUrl
                                    : s.Images.Where(i => !i.IsDeleted)
                                              .OrderBy(i => i.CreatedAt)
                                              .Select(i => i.ImageUrl)
                                              .FirstOrDefault()))
                            .ForMember(d => d.Amenities, opt => opt.MapFrom(s =>
                                s.Amenities.Select(k => k.Word).ToList()));

            CreateMap<ListingImage, ListingImageVM>()
                            .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id))
                            .ForMember(d => d.ImageUrl, opt => opt.MapFrom(s => s.ImageUrl))
                            .ForMember(d => d.IsMain, opt => opt.MapFrom(s =>
                                s.Listing.MainImageId == s.Id));

            // ListingDetailVM -> ListingUpdateVM
            CreateMap<ListingDetailVM, ListingUpdateVM>()
                .ForMember(d => d.NewImages, o => o.Ignore())
                .ForMember(d => d.RemoveImageIds, o => o.Ignore())
                .ForMember(d => d.Amenities, o => o.MapFrom(s => s.Amenities));

            CreateMap<Listing, ListingUpdateVM>()
                // simple scalar props (adjust names if your VM uses different ones)
                .ForMember(d => d.Title, opt => opt.MapFrom(s => s.Title))
                .ForMember(d => d.Description, opt => opt.MapFrom(s => s.Description))
                .ForMember(d => d.PricePerNight, opt => opt.MapFrom(s => s.PricePerNight))
                .ForMember(d => d.Location, opt => opt.MapFrom(s => s.Location))
                .ForMember(d => d.Latitude, opt => opt.MapFrom(s => s.Latitude))
                .ForMember(d => d.Longitude, opt => opt.MapFrom(s => s.Longitude))
                .ForMember(d => d.MaxGuests, opt => opt.MapFrom(s => s.MaxGuests))

                // keywords: map collection of Amenity -> List<string>
                .ForMember(d => d.Amenities, opt => opt.MapFrom(s => s.Amenities.Select(k => k.Word).ToList()))


                // ignore fields that are only for client uploads/changes
                .ForMember(d => d.NewImages, opt => opt.Ignore())
                .ForMember(d => d.RemoveImageIds, opt => opt.Ignore());
            // messages
            CreateMap<Message, GetMessageVM>().ReverseMap();
            CreateMap<Message, CreateMessageVM>().ReverseMap();
        }


    }
}
