
using BLL.AutoMapper;
using BLL.Common;
using DAL.Common;
using DAL.Database;
using DAL.Entities;
using DAL.Enum;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace PL
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // DbContext
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Configure Identity integration (creates all authentication tables)
            builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
            {
                // optional basic config (you can change later)
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
            })
            .AddEntityFrameworkStores<AppDbContext>();


            // add modular in program
            builder.Services.AddBuissinesInBLL();
            builder.Services.AddBuissinesInDAL();
            builder.Services.AddAutoMapper(cfg => cfg.AddProfile<ListingProfile>());//AutoMapperForListing BLL





            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();
            app.UseStaticFiles();

            await AppDbInitializer.SeedAsync(app);

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            // Identity middlewares
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }

    public static class AppDbInitializer
    {
        public static async Task SeedAsync(IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Roles
            string[] roles = { "Admin", "Host", "Guest" };
            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                    await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            }

            // Admin user
            var adminEmail = "admin@airbnbclone.com";
            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                // Create Admin user
                admin = User.Create(
                    fullName: "System Admin",
                    role: UserRole.Admin
                );

                admin = User.Create("System Admin", UserRole.Admin);


                // Set Identity fields manually
                admin.Email = adminEmail;
                admin.UserName = adminEmail;
                admin.NormalizedEmail = adminEmail.ToUpper();
                admin.NormalizedUserName = adminEmail.ToUpper();

                var result = await userManager.CreateAsync(admin, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }

            // Sample Amenities
            if (!context.Amenities.Any())
            {
                var wifi = Amenity.Create("Wi-Fi");
                var pool = Amenity.Create("Pool");
                var ac = Amenity.Create("Air Conditioning");

                context.Amenities.AddRange(wifi, pool, ac);
            }

            // Sample Keywords
            if (!context.Keywords.Any())
            {
                var beach = Keyword.Create("Beach");
                var luxury = Keyword.Create("Luxury");

                context.Keywords.AddRange(beach, luxury);
            }

            await context.SaveChangesAsync();

            // Sample Listings
            if (!context.Listings.Any())
            {
                var adminId = admin.Id;

                // Retrieve amenities & keywords from context
                var wifiAmenity = context.Amenities.First(a => a.Name == "Wi-Fi");
                var poolAmenity = context.Amenities.First(a => a.Name == "Pool");
                var acAmenity = context.Amenities.First(a => a.Name == "Air Conditioning");

                var beachKeyword = context.Keywords.First(k => k.Word == "Beach");
                var luxuryKeyword = context.Keywords.First(k => k.Word == "Luxury");

                // Listing 1
                var listing1 = Listing.Create(
                    title: "Beach Villa",
                    description: "Luxury villa near the beach.",
                    pricePerNight: 250,
                    location: "California",
                    latitude: 34.0195,
                    longitude: -118.4912,
                    maxGuests: 6,
                    userId: adminId,
                    tags: new List<string> { "beach", "villa", "luxury" },//
                    createdBy: "System Admin"//

                );
                listing1.Amenities.Add(wifiAmenity);
                listing1.Amenities.Add(poolAmenity);
                listing1.Keywords.Add(beachKeyword);
                listing1.Images.Add(ListingImage.Create(listing1.Id, "https://example.com/beach-villa.jpg"));

                // Listing 2
                var listing2 = Listing.Create(
                    title: "City Apartment",
                    description: "Modern apartment in the city center.",
                    pricePerNight: 120,
                    location: "New York",
                    latitude: 40.7128,
                    longitude: -74.0060,
                    maxGuests: 4,
                    userId: adminId,
                    tags: new List<string> { "city", "apartment", "modern" },//
                    createdBy: "System Admin"//
                );
                listing2.Amenities.Add(wifiAmenity);
                listing2.Amenities.Add(acAmenity);
                listing2.Keywords.Add(luxuryKeyword);
                listing2.Images.Add(ListingImage.Create(listing2.Id, "https://example.com/city-apartment.jpg"));

                context.Listings.AddRange(listing1, listing2);
                await context.SaveChangesAsync();
            }
        }
    }
}
