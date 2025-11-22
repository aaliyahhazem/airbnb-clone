
namespace BLL.AutoMapper
{
    internal class DomainProfile : Profile
    {
        public DomainProfile() 
        {
            // notifications
            CreateMap<Notification, GetNotificationVM>().ReverseMap();
            CreateMap<Notification, CreateNotificationVM>().ReverseMap();
            // messages
            CreateMap<Message, GetMessageVM>().ReverseMap();
            CreateMap<Message, CreateMessageVM>().ReverseMap();
            // reviews
            CreateMap<Review, CreateReviewVM>().ReverseMap();
            // booking
            CreateMap<Booking, CreateBookingVM>().ReverseMap();
        }

    }
}
