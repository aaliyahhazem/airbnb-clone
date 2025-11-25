using Stripe;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BLL.Common;
using DAL.Common;
using DAL.Database;
using DAL.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PL.Hubs;

namespace PL
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Keep original JWT claim types (don't remap sub/name, etc.)
            JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

            // Configure Stripe Settings
            builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));

            var stripeSettings = builder.Configuration.GetSection("Stripe").Get<StripeSettings>();
            StripeConfiguration.ApiKey = stripeSettings.SecretKey;

            // ----------------------------------------------------------------------------
            // CORS
            // Allow the SPA frontend (Angular on localhost:4200) to access this API
            // and allow credentials for SignalR connections.
            // ----------------------------------------------------------------------------
            // --------------------------------------------------------------------
            // CORS (allow Angular frontend on localhost:4200)
            // --------------------------------------------------------------------
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins("http://localhost:4200")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            // --------------------------------------------------------------------
            // DbContext
            // --------------------------------------------------------------------
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // --------------------------------------------------------------------
            // Identity
            // --------------------------------------------------------------------
            builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
            {
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            // --------------------------------------------------------------------
            // Authentication (JWT + external providers)
            // --------------------------------------------------------------------
            var authBuilder = builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            });

            var jwtKey = builder.Configuration["Jwt:Key"];
            var jwtIssuer = builder.Configuration["Jwt:Issuer"];
            var jwtAudience = builder.Configuration["Jwt:Audience"];

            if (!string.IsNullOrWhiteSpace(jwtKey))
            {
                authBuilder.AddJwtBearer(options =>
                {
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtIssuer,
                        ValidateAudience = true,
                        ValidAudience = jwtAudience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(20),

                        NameClaimType = JwtRegisteredClaimNames.Sub,
                        RoleClaimType = ClaimTypes.Role
                    };

                    // Allow SignalR to read token from query string on hubs
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;

                            if (!string.IsNullOrEmpty(accessToken) &&
                                (path.StartsWithSegments("/notificationsHub") ||
                                 path.StartsWithSegments("/messagesHub")))
                            {
                                context.Token = accessToken;
                            }

                            return Task.CompletedTask;
                        }
                    };
                });
            }

            // Google auth (optional)
            var googleId = builder.Configuration["Authentication:Google:ClientId"];
            var googleSecret = builder.Configuration["Authentication:Google:ClientSecret"];
            if (!string.IsNullOrWhiteSpace(googleId) && !string.IsNullOrWhiteSpace(googleSecret))
            {
                authBuilder.AddGoogle(opts =>
                {
                    opts.ClientId = googleId;
                    opts.ClientSecret = googleSecret;
                });
            }

            // Facebook auth (optional)
            var fbId = builder.Configuration["Authentication:Facebook:AppId"];
            var fbSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
            if (!string.IsNullOrWhiteSpace(fbId) && !string.IsNullOrWhiteSpace(fbSecret))
            {
                authBuilder.AddFacebook(opts =>
                {
                    opts.AppId = fbId;
                    opts.AppSecret = fbSecret;
                });
            }

            // --------------------------------------------------------------------
            // DAL/BLL registrations
            // --------------------------------------------------------------------
            builder.Services.AddBuissinesInDAL();
            builder.Services.AddBuissinesInBLL();

            // Make sure AdminRepository exists
            builder.Services.AddScoped<DAL.Repo.Abstraction.IAdminRepository, DAL.Repo.Implementation.AdminRepository>();

            // AutoMapper � scan all assemblies for profiles
            //builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            // Controllers & SignalR
            builder.Services.AddControllers();
            builder.Services.AddSignalR();

            // Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // --------------------------------------------------------------------
            // Middleware pipeline
            // --------------------------------------------------------------------
            app.UseCors("AllowFrontend");

            app.UseStaticFiles();

            // Seed roles, admin, sample data
            await AppDbInitializer.SeedAsync(app);

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.MapHub<NotificationHub>("/notificationsHub");
            app.MapHub<MessageHub>("/messagesHub");

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

            // ----------------- Roles -----------------
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
                    // optional: log
                }
            }

            // ----------------- Admin user -----------------
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

                    admin.Email = adminEmail;
                    admin.UserName = adminEmail;
                    admin.NormalizedEmail = adminEmail.ToUpperInvariant();
                    admin.NormalizedUserName = adminEmail.ToUpperInvariant();

                    var result = await userManager.CreateAsync(admin, "Admin@123");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(admin, "Admin");
                    }
                }
                catch
                {
                    // optional: log
                }
            }

            await context.SaveChangesAsync();

            // ----------------- Sample Listings -----------------
            if (!context.Listings.Any())
            {
                var adminId = admin?.Id ?? Guid.Empty;
                if (adminId == Guid.Empty) return;

                // Existing amenities (if any)
                var wifiAmenity = context.Amenities.FirstOrDefault(a => a.Word == "Wi-Fi");
                var poolAmenity = context.Amenities.FirstOrDefault(a => a.Word == "Pool");
                var acAmenity = context.Amenities.FirstOrDefault(a => a.Word == "Air Conditioning");
                var luxuryAmenity = context.Amenities.FirstOrDefault(a => a.Word == "Luxury");

                using var tx = await context.Database.BeginTransactionAsync();
                try
                {
                    // ---- Listing 1 ----
                    var listing1 = Listing.Create(
                        title: "City Apartment",
                        description: "Modern apartment...",
                        pricePerNight: 120m,
                        location: "Cairo, Egypt",
                        latitude: 30.0444,
                        longitude: 31.2357,
                        maxGuests: 4,
                        userId: adminId,
                        createdBy: "System Admin",
                        mainImageUrl: ""
                    );

                    context.Listings.Add(listing1);
                    await context.SaveChangesAsync(); // listing1.Id

                    var img1 = ListingImage.CreateImage(listing1, "https://example.com/city.jpg", "System Admin");
                    context.ListingImages.Add(img1);
                    await context.SaveChangesAsync();

                    listing1.SetMainImage(img1, "System Admin");
                    await context.SaveChangesAsync();

                    if (!context.Amenities.Any())
                    {
                        var wifi = Amenity.Create("Wi-Fi", listing1);
                        var pool = Amenity.Create("Pool", listing1);

                        context.Amenities.AddRange(wifi, pool);
                        await context.SaveChangesAsync();
                    }

                    // ---- Listing 2 ----
                    var listing2 = Listing.Create(
                        title: "City Apartment 2",
                        description: "Modern apartment in the city center.",
                        pricePerNight: 130m,
                        location: "Cairo, Egypt",
                        latitude: 30.0500,
                        longitude: 31.2500,
                        maxGuests: 4,
                        userId: adminId,
                        createdBy: "System Admin",
                        mainImageUrl: string.Empty
                    );

                    if (wifiAmenity != null) listing2.Amenities.Add(wifiAmenity);
                    if (acAmenity != null) listing2.Amenities.Add(acAmenity);
                    if (luxuryAmenity != null) listing2.Amenities.Add(luxuryAmenity);

                    context.Listings.Add(listing2);
                    await context.SaveChangesAsync();

                    var img2 = ListingImage.CreateImage(listing2, "https://example.com/city-apartment.jpg", "System Admin");
                    context.ListingImages.Add(img2);
                    await context.SaveChangesAsync();

                    listing2.SetMainImage(img2, "System Admin");
                    await context.SaveChangesAsync();

                    // Mark all seeded listings as approved
                    await context.Database.ExecuteSqlRawAsync(
                        "UPDATE Listings SET IsApproved = 1, IsReviewed = 1 WHERE UserId = {0}",
                        adminId
                    );

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
