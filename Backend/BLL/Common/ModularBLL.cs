



namespace BLL.Common
{
    public static class ModularBLL
    {
        public static IServiceCollection AddBuissinesInBLL(this IServiceCollection services)
        {
            //notifiaction
            services.AddScoped<INotificationService, NotificationService>();
             
            services.AddAutoMapper(x => x.AddProfile(new DomainProfile()));
            return services;
        }
    }
}
