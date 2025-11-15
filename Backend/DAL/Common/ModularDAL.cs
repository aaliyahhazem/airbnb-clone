

using DAL.Repo.Implementation;
using Microsoft.Extensions.DependencyInjection;

namespace DAL.Common
{
    public static class ModularDAL
    {
        public static IServiceCollection AddBuissinesInDAL(this IServiceCollection services)
        {
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IListingRepository, ListingRepository>();
            services.AddScoped<IBookingRepository, BookingRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IReviewRepository, ReviewRepository>();
            services.AddScoped<IMessageRepository, MessageRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IListingImageRepository, ListingImageRepository>();
            services.AddScoped<IAmenityRepository, AmenityRepository>();
            services.AddScoped<IKeywordRepository, KeywordRepository>();
            return services;
        }
    }
}
