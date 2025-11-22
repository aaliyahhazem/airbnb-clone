using Microsoft.Extensions.Http;

namespace BLL.Common
{
    public static class ModularBLL
    {
        public static IServiceCollection AddBuissinesInBLL(this IServiceCollection services)
        {
            //notifiaction
            services.AddScoped<INotificationService, NotificationService>();
            // messages
            services.AddScoped<IMessageService, MessageService>();
            // identity
            services.AddScoped<IIdentityService, IdentityService>();
            // reviews
            services.AddScoped<IReviewService, ReviewService>();
            // admin
            services.AddScoped<IAdminService, AdminService>();
            
            services.AddScoped<IListingService, ListingService>();
            services.AddScoped<IListingImageService, ListingImageService>();

            services.AddScoped<IMapService, MapService>();
            services.AddHttpClient<IGeocodingService, NominatimGeocodingService>();
            services.AddAutoMapper(x => x.AddProfile(new DomainProfile()));
            // Token service
            services.AddSingleton<ITokenService, TokenService>();
            // Ensure IdentityService is registered with token service injected
            services.AddScoped<IIdentityService, IdentityService>();
            return services;
        }
    }
}
