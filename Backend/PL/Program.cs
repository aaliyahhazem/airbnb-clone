using BLL.Common;
using DAL.Common;
using DAL.Database;
using DAL.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using PL.Hubs;


namespace PL
{
    public class Program 
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. ????? ????? CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins("http://localhost:4200") // ??? ?????? ???? Angular
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials(); // ?? ??????? SignalR
                });
            });



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
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            // External authentication providers - register only when credentials exist
            // JWT as default auth scheme
            var authBuilder = builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            });

            // Configure JWT validation
            var jwtKey = builder.Configuration["Jwt:Key"];
            var jwtIssuer = builder.Configuration["Jwt:Issuer"];
            var jwtAudience = builder.Configuration["Jwt:Audience"];
            if (!string.IsNullOrWhiteSpace(jwtKey))
            {
                authBuilder.AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtIssuer,
                        ValidateAudience = true,
                        ValidAudience = jwtAudience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(2)
                    };
                    // Allow SignalR to read token from querystring for hubs
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken) && (path.StartsWithSegments("/notificationsHub") || path.StartsWithSegments("/messagesHub")))
                            {
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });
            }

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

            // add modular in program
            // ensure DAL registrations occur before BLL so required repos are available
            builder.Services.AddBuissinesInDAL();
            builder.Services.AddBuissinesInBLL();
            // safe-guard: ensure IAdminRepository is registered (some extension variants may not register it)
            builder.Services.AddScoped<DAL.Repo.Abstraction.IAdminRepository, DAL.Repo.Implementation.AdminRepository>();








            builder.Services.AddControllers();

            //signalR
            builder.Services.AddSignalR();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            app.UseCors("AllowFrontend");

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

            //signal R
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
         
            await context.SaveChangesAsync();

            // Sample Listings - create in safe two-step way to avoid FK circular insert issues
            if (!context.Listings.Any())
            {
                // fetch admin id (if null, skip)
                var adminId = admin?.Id ?? Guid.Empty;
                if (adminId == Guid.Empty) return;

                // get reference entities (no AsNoTracking because we'll attach them)
                var wifiAmenity = context.Amenities.FirstOrDefault(a => a.Word == "Wi-Fi");
                var poolAmenity = context.Amenities.FirstOrDefault(a => a.Word == "Pool");
                var acAmenity = context.Amenities.FirstOrDefault(a => a.Word == "Air Conditioning");

                var beachAmenity = context.Amenities.FirstOrDefault(k => k.Word == "Beach");
                var luxuryAmenity = context.Amenities.FirstOrDefault(k => k.Word == "Luxury");

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
                        userId: adminId,
                        createdBy: "System Admin",
                        mainImageUrl: string.Empty
                        
                    );

                    // Seed Amenities
                    try
                    {
                        if (!context.Amenities.Any())
                        {
                            var wifi = Amenity.Create("Wi-Fi" , listing);
                            var pool = Amenity.Create("Pool" , listing2);

                            context.Amenities.AddRange(wifi, pool);
                        }
                    }
                    catch { /* ignore */ }
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