using Microsoft.Extensions.DependencyInjection;
using DAL.Repo.Abstraction;
using DAL.Repo.Implementation;

namespace DAL.Common
{
    public static class ModularDataAccessLayer
    {
        // This class serves as a modular data access layer for various database operations.
        public static IServiceCollection AddBussinesInDAL(this IServiceCollection services)
        {
            services.AddScoped<IListingRepository, ListingRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IBookingRepository, BookingRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IReviewRepository, ReviewRepository>();
            services.AddScoped<IMessageRepository, MessageRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IListingImageRepository, ListingImageRepository>();
            services.AddScoped<IAmenityRepository, AmenityRepository>();
            services.AddScoped<IAmenityRepository, AmenityRepository>();
            services.AddScoped<IMapRepo, MapRepo>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            // admin (register abstraction and implementation)
            services.AddScoped<IAdminRepository, AdminRepository>();
            return services;
        }

    }
}
