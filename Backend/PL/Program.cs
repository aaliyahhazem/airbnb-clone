
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

            // Identity
            builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
            {
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
            })
            .AddEntityFrameworkStores<AppDbContext>();

            // AutoMapper
            builder.Services.AddAutoMapper(cfg => cfg.AddProfile<DomainProfile>());

            // Register DAL + BLL services (either inline or via extension methods)
            builder.Services.AddBuissinesInDAL(); // implement in DAL — registers repos & UoW
            builder.Services.AddBuissinesInBLL(); // implement in BLL — registers services

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();
            app.UseStaticFiles();

            // seed database (ensure this runs after app is built so DI scope is available)
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
                try
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                        await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                }
                catch
                {
                    // ignore seed role errors (optional: log)
                }
            }

            // Admin user
            var adminEmail = "admin@airbnbclone.com";
            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                try
                {
                    admin = User.Create(
                        fullName: "System Admin",
                        role: DAL.Enum.UserRole.Admin
                    );

                    // Identity fields
                    admin.Email = adminEmail;
                    admin.UserName = adminEmail;
                    admin.NormalizedEmail = adminEmail.ToUpperInvariant();
                    admin.NormalizedUserName = adminEmail.ToUpperInvariant();

                    var result = await userManager.CreateAsync(admin, "Admin@123");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(admin, "Admin");
                    }
                    // else ignore for seed
                }
                catch
                {
                    // ignore seed user errors (optional: log)
                }
            }

            // Seed Amenities
            try
            {
                if (!context.Amenities.Any())
                {
                    var wifi = Amenity.Create("Wi-Fi");
                    var pool = Amenity.Create("Pool");
                    var ac = Amenity.Create("Air Conditioning");

                    context.Amenities.AddRange(wifi, pool, ac);
                }
            }
            catch { /* ignore */ }

            // Seed Keywords
            try
            {
                if (!context.Keywords.Any())
                {
                    var beach = Keyword.Create("Beach");
                    var luxury = Keyword.Create("Luxury");

                    context.Keywords.AddRange(beach, luxury);
                }
            }
            catch { /* ignore */ }

            await context.SaveChangesAsync();

            // Sample Listings - create in safe two-step way to avoid FK circular insert issues
            if (!context.Listings.Any())
            {
                // fetch admin id (if null, skip)
                var adminId = admin?.Id ?? Guid.Empty;
                if (adminId == Guid.Empty) return;

                // get reference entities (no AsNoTracking because we'll attach them)
                var wifiAmenity = context.Amenities.FirstOrDefault(a => a.Name == "Wi-Fi");
                var poolAmenity = context.Amenities.FirstOrDefault(a => a.Name == "Pool");
                var acAmenity = context.Amenities.FirstOrDefault(a => a.Name == "Air Conditioning");

                var beachKeyword = context.Keywords.FirstOrDefault(k => k.Word == "Beach");
                var luxuryKeyword = context.Keywords.FirstOrDefault(k => k.Word == "Luxury");

                // Use a transaction to keep seed atomic-ish
                using var tx = await context.Database.BeginTransactionAsync();
                try
                {
                    // 1) create listing without setting main image id
                    var listing = Listing.Create(
                        title: "City Apartment",
                        description: "Modern apartment...",
                        pricePerNight: 120m,
                        location: "New York",
                        latitude: 40.7128,
                        longitude: -74.0060,
                        maxGuests: 4,
                        tags: new List<string> { "city", "apartment" },
                        userId: adminId,
                        createdBy: "System Admin",
                        mainImageUrl: null // create images separately
                    );

                    context.Listings.Add(listing);
                    await context.SaveChangesAsync(); // listing.Id assigned

                    // 2) create image and persist
                    var img = ListingImage.CreateImage(listing, "https://example.com/city.jpg", "System Admin");
                    context.ListingImages.Add(img);
                    await context.SaveChangesAsync(); // img.Id assigned

                    // 3) set main image now that img.Id exists, and persist
                    listing.SetMainImage(img.Id, "System Admin");
                    await context.SaveChangesAsync();
                    // ---- Listing 2 (City Apartment) ----
                    var listing2 = Listing.Create(
                        title: "City Apartment",
                        description: "Modern apartment in the city center.",
                        pricePerNight: 120m,
                        location: "New York",
                        latitude: 40.7128,
                        longitude: -74.0060,
                        maxGuests: 4,
                        tags: new List<string> { "city", "apartment", "modern" },
                        userId: adminId,
                        createdBy: "System Admin",
                        mainImageUrl: string.Empty
                    );

                    if (wifiAmenity != null) listing2.Amenities.Add(wifiAmenity);
                    if (acAmenity != null) listing2.Amenities.Add(acAmenity);
                    if (luxuryKeyword != null) listing2.Keywords.Add(luxuryKeyword);

                    context.Listings.Add(listing2);
                    await context.SaveChangesAsync();

                    var img2 = ListingImage.CreateImage(listing2, "https://example.com/city-apartment.jpg", "System Admin");
                    context.ListingImages.Add(img2);
                    await context.SaveChangesAsync();

                    listing2.SetMainImage(img2, "System Admin");
                    await context.SaveChangesAsync();

                    await tx.CommitAsync();
                }
                catch
                {
                    try { await tx.RollbackAsync(); } catch { /* ignore */ }
                }
            }
        }
    }
}