namespace BLL.Common
{
    public static class ModularBLL
    {
        public static IServiceCollection AddBuissinesInBLL(this IServiceCollection services)
        {
            // notification
            services.AddScoped<INotificationService, NotificationService>();
            // messages
            services.AddScoped<IMessageService, MessageService>();
            // identity
            services.AddScoped<IIdentityService, Services.Impelementation.IdentityService>();
            // reviews
            services.AddScoped<IReviewService, Services.Impelementation.ReviewService>();
            // admin
            services.AddScoped<IAdminService, AdminService>();
            
            services.AddScoped<IListingService, ListingService>();
            services.AddScoped<IListingImageService, ListingImageService>();

            services.AddScoped<IMapService, MapService>();
            services.AddHttpClient<IGeocodingService, NominatimGeocodingService>();
            // bookings & payments
            services.AddScoped<IBookingService, BookingService>();
            services.AddScoped<IPaymentService, PaymentService>();

            services.AddAutoMapper(x => x.AddProfile(new DomainProfile()));
            // Token service
            services.AddSingleton<ITokenService, Services.Impelementation.TokenService>();
            // Ensure IdentityService is registered with token service injected
            //services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<IFavoriteService, FavoriteService>();
            return services;
        }
    }
}
