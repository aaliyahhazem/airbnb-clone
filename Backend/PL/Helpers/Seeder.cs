using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using DAL.Entities;
using DAL.Repo.Abstraction;
using System.Text.RegularExpressions;

namespace PL.Helpers
{
    public static class Seeder
    {
        public static async Task SeedIfNeededAsync(IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var services = scope.ServiceProvider;

            var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var userManager = services.GetRequiredService<UserManager<User>>();
            var uow = services.GetRequiredService<IUnitOfWork>();

            // check if already seeded by looking for admin1
            var admin1Email = "admin1@airbnbclone.com";
            var existing = await userManager.FindByEmailAsync(admin1Email);
            if (existing != null)
                return; // already seeded

            // ensure roles
            string[] roles = { "Admin", "Host", "Guest" };
            foreach (var r in roles)
            {
                if (!await roleManager.RoleExistsAsync(r))
                    await roleManager.CreateAsync(new IdentityRole<Guid>(r));
            }

            // create admins
            var admin1 = User.Create("Admin One", DAL.Enum.UserRole.Admin);
            admin1.Email = admin1Email;
            admin1.UserName = "admin1";
            await userManager.CreateAsync(admin1, "Admin@123");
            await userManager.AddToRoleAsync(admin1, "Admin");

            var admin2 = User.Create("Admin Two", DAL.Enum.UserRole.Admin);
            admin2.Email = "admin2@airbnbclone.com";
            admin2.UserName = "admin2";
            await userManager.CreateAsync(admin2, "Admin@123");
            await userManager.AddToRoleAsync(admin2, "Admin");

            // create regular users
            var users = new List<User>();
            for (int i = 1; i <= 3; i++)
            {
                var email = $"user{i}@gmail.com";
                var u = User.Create($"User {i}", DAL.Enum.UserRole.Guest);
                u.Email = email;
                // sanitize username
                var baseName = Regex.Replace($"user{i}", "[^a-zA-Z0-9]", string.Empty);
                u.UserName = string.IsNullOrWhiteSpace(baseName) ? Guid.NewGuid().ToString("N") : baseName;
                await userManager.CreateAsync(u, "user123");
                await userManager.AddToRoleAsync(u, "Guest");
                users.Add(u);
            }

            // seed20 listings
            var rnd = new Random();
            var owners = new List<User> { admin1, admin2 }.Concat(users).ToList();
            var listings = new List<Listing>();
            for (int i = 1; i <= 20; i++)
            {
                var owner = owners[rnd.Next(owners.Count)];
                var listing = Listing.Create(
                title: $"Sample Listing {i}",
                description: "Seeded listing",
                pricePerNight: 50 + rnd.Next(0, 200),
                location: "City Center",
                latitude: 30 + rnd.NextDouble(),
                longitude: 31 + rnd.NextDouble(),
                maxGuests: 1 + rnd.Next(1, 6),
                userId: owner.Id,
                createdBy: owner.FullName,
                mainImageUrl: string.Empty,
                keywordNames: new List<string> { "wifi", "parking" }
                );

                await uow.Listings.AddAsync(listing);
                listings.Add(listing);
            }

            await uow.SaveChangesAsync();

            // seed5 bookings
            for (int b = 0; b < 5; b++)
            {
                var listing = listings[rnd.Next(listings.Count)];
                var guest = owners.Where(u => u.Id != listing.UserId).OrderBy(x => rnd.Next()).First();
                var checkIn = DateTime.UtcNow.AddDays(rnd.Next(1, 60));
                var checkOut = checkIn.AddDays(rnd.Next(1, 7));
                var nights = (decimal)(checkOut - checkIn).TotalDays;
                var total = nights * listing.PricePerNight;

                var booking = await uow.Bookings.CreateAsync(listing.Id, guest.Id, checkIn, checkOut, total);
                await uow.SaveChangesAsync();

                // create a payment for the booking
                var payment = DAL.Entities.Payment.Create(booking.Id, booking.TotalPrice, "card", Guid.NewGuid().ToString(), DAL.Enum.PaymentStatus.Pending, DateTime.UtcNow);
                await uow.Payments.AddAsync(payment);
                await uow.SaveChangesAsync();
            }

            // seed messages
            for (int m = 0; m < 20; m++)
            {
                var sender = owners[rnd.Next(owners.Count)];
                var receiver = owners.Where(u => u.Id != sender.Id).OrderBy(x => rnd.Next()).First();
                await uow.Messages.CreateAsync(sender.Id, receiver.Id, $"Hello from {sender.FullName} (msg {m})", DateTime.UtcNow.AddMinutes(-m), false);
            }

            // seed notifications
            var allUsers = owners;
            foreach (var u in allUsers)
            {
                await uow.Notifications.CreateAsync(u.Id, "Welcome", $"Welcome {u.FullName}", DAL.Enum.NotificationType.System);
            }

            await uow.SaveChangesAsync();
        }
    }
}
