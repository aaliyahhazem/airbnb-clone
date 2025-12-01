
using BLL.ModelVM.LIstingVM;
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


            //listing -> HomepageVM
            CreateMap<Listing, HomeVM>()
                .ForMember(d => d.ImageUrl, opt => opt.MapFrom(s => s.MainImage != null
                       ? $"/listings/{s.MainImage.ImageUrl}"
                       : s.Images.Where(i => !i.IsDeleted).OrderBy(i => i.Id).Select(i => $"/listings/{i.ImageUrl}").FirstOrDefault()));

            // Listing -> OverviewVM
            CreateMap<Listing, ListingOverviewVM>()
            .ForMember(d => d.MainImageUrl, opt => opt.MapFrom(s => s.MainImage != null
                       ? $"/listings/{s.MainImage.ImageUrl}"
                       : s.Images.Where(i => !i.IsDeleted).OrderBy(i => i.Id).Select(i => $"/listings/{i.ImageUrl}").FirstOrDefault()))
                        .ForMember(d => d.Amenities, opt => opt.MapFrom(s => s.Amenities.Select(k => k.Word)))
                        .ForMember(d => d.Bedrooms, opt => opt.MapFrom(s => s.NumberOfRooms))
                        .ForMember(d => d.Bathrooms, opt => opt.MapFrom(s => s.NumberOfBathrooms))
                        .ForMember(d => d.Priority, opt => opt.MapFrom(s => s.Priority))
                        .ForMember(d => d.ViewCount, opt => opt.MapFrom(s => s.ViewCount))
                        .ForMember(d => d.FavoriteCount, opt => opt.MapFrom(s => s.FavoriteCount))
                        .ForMember(d => d.BookingCount, opt => opt.MapFrom(s => s.BookingCount));


            /// Listing → ListingDetailVM
            CreateMap<Listing, ListingDetailVM>()
                            .ForMember(d => d.Images, opt => opt.MapFrom(s =>
                                s.Images.Where(i => !i.IsDeleted)
                                        .OrderBy(i => i.CreatedAt)))
                            .ForMember(d => d.MainImageUrl, opt => opt.MapFrom(s =>
                                s.MainImage != null
                                    ? $"/listings/{s.MainImage.ImageUrl}"
                                    : s.Images.Where(i => !i.IsDeleted)
                                              .OrderBy(i => i.CreatedAt)
                                              .Select(i => $"/listings/{i.ImageUrl}")
                                              .FirstOrDefault()))
                            .ForMember(d => d.Amenities, opt => opt.MapFrom(s =>
                                s.Amenities.Select(k => k.Word).ToList()))
                            .ForMember(d => d.  Bedrooms, opt => opt.MapFrom(s => s.NumberOfRooms))
                            .ForMember(d => d.Bathrooms, opt => opt.MapFrom(s => s.NumberOfBathrooms))
                            .ForMember(d => d.Priority, opt => opt.MapFrom(s => s.Priority))
                            .ForMember(d => d.ViewCount, opt => opt.MapFrom(s => s.ViewCount))
                            .ForMember(d => d.FavoriteCount, opt => opt.MapFrom(s => s.FavoriteCount))
                            .ForMember(d => d.BookingCount, opt => opt.MapFrom(s => s.BookingCount));

            CreateMap<ListingImage, ListingImageVM>()
                            .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id))
                            .ForMember(d => d.ImageUrl, opt => opt.MapFrom(s => $"/listings/{s.ImageUrl}"))
                            .ForMember(d => d.IsMain, opt => opt.MapFrom(s =>
                                s.Listing.MainImageId == s.Id));

            // Scalars
            CreateMap<Listing, ListingUpdateVM>()
                 .ForMember(d => d.Title, opt => opt.MapFrom(s => s.Title))
                 .ForMember(d => d.Description, opt => opt.MapFrom(s => s.Description))
                 .ForMember(d => d.PricePerNight, opt => opt.MapFrom(s => s.PricePerNight))
                 .ForMember(d => d.Location, opt => opt.MapFrom(s => s.Location))
                 .ForMember(d => d.Latitude, opt => opt.MapFrom(s => s.Latitude))
                 .ForMember(d => d.Longitude, opt => opt.MapFrom(s => s.Longitude))
                 .ForMember(d => d.MaxGuests, opt => opt.MapFrom(s => s.MaxGuests))
                 .ForMember(d => d.Destination, opt => opt.MapFrom(s => s.Destination))
                 .ForMember(d => d.Type, opt => opt.MapFrom(s => s.Type))
                 .ForMember(d => d.NumberOfRooms, opt => opt.MapFrom(s => s.NumberOfRooms))
                 .ForMember(d => d.NumberOfBathrooms, opt => opt.MapFrom(s => s.NumberOfBathrooms))

                 // Amenities
                 .ForMember(d => d.Amenities,
                     opt => opt.MapFrom(s => s.Amenities.Select(a => a.Word).ToList()))

                 // ALL images
                 .ForMember(d => d.Images,
                     opt => opt.MapFrom(s => s.Images.Where(i => !i.IsDeleted)))

                 // Main image
                 .ForMember(d => d.MainImage,
                     opt => opt.MapFrom(s =>
                         s.MainImage != null
                             ? s.MainImage
                             : s.Images.Where(i => !i.IsDeleted).OrderBy(i => i.Id).FirstOrDefault()))

                 // Ignore inputs
                 .ForMember(d => d.NewImages, opt => opt.Ignore())
                 .ForMember(d => d.RemoveImageIds, opt => opt.Ignore());
            // messages
            CreateMap<Message, GetMessageVM>().ReverseMap();
            CreateMap<Message, CreateMessageVM>().ReverseMap();
            CreateMap<DAL.Dto.ConversationDto, ConversationVM>().ReverseMap();
            // reviews
            CreateMap<DAL.Entities.Review, CreateReviewVM>().ReverseMap();
            // booking
            CreateMap<Booking, CreateBookingVM>().ReverseMap();
        }


    }
}
