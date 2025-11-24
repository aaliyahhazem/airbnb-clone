using Stripe;

namespace PL
{
    // Program: application entrypoint and host configuration
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Preserve original incoming JWT claims (do not map 'sub' to name)
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
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins("http://localhost:4200")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials(); // required for SignalR when using cookies or auth headers
                });
            });

            // ----------------------------------------------------------------------------
            // Database
            // Register the EF Core DbContext using SQL Server (connection string from config)
            // ----------------------------------------------------------------------------
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // ----------------------------------------------------------------------------
            // Identity (ASP.NET Core Identity)
            // Configures user/role stores and default token providers
            // ----------------------------------------------------------------------------
            builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
            {
                // Basic password policy (can be tightened later)
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            // ----------------------------------------------------------------------------
            // Authentication: JWT is the default scheme. External providers (Google, Facebook)
            // are added only when credentials exist in configuration.
            // ----------------------------------------------------------------------------
            var authBuilder = builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            });

            // Configure JWT validation parameters
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
                        ClockSkew = TimeSpan.FromMinutes(2),

                        // Keep original claim types for name/role
                        NameClaimType = JwtRegisteredClaimNames.Sub,
                        RoleClaimType = ClaimTypes.Role
                    };

                    // Allow SignalR hubs to receive the access token from the query string
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;

                            // If the request is for one of the hubs, read token from query string
                            if (!string.IsNullOrEmpty(accessToken) &&
                                (path.StartsWithSegments("/notificationsHub") || path.StartsWithSegments("/messagesHub")))
                            {
                                context.Token = accessToken;
                            }

                            return Task.CompletedTask;
                        }
                    };
                });
            }

            // Google external authentication (if configured)
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

            // Facebook external authentication (if configured)
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

            // ----------------------------------------------------------------------------
            // Application services registration (modular)
            // Ensure DAL registrations before BLL so repositories are available.
            // ----------------------------------------------------------------------------
            builder.Services.AddBuissinesInDAL();
            builder.Services.AddBuissinesInBLL();

            // Explicit service registrations as safeguards
            builder.Services.AddScoped<IBookingService, BookingService>();
            builder.Services.AddScoped<IPaymentService, PaymentService>();

            // Ensure IAdminRepository registration (some extension variants may not register it)
            builder.Services.AddScoped<DAL.Repo.Abstraction.IAdminRepository, DAL.Repo.Implementation.AdminRepository>();

            // AutoMapper profiles (e.g. ListingProfile in BLL)
            builder.Services.AddAutoMapper(cfg => cfg.AddProfile<ListingProfile>());

            // MVC controllers
            builder.Services.AddControllers();

            // SignalR for real-time features (notifications, messages)
            builder.Services.AddSignalR();

            // Swagger/OpenAPI
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // ----------------------------------------------------------------------------
            // Build the app and configure middleware pipeline
            // ----------------------------------------------------------------------------
            var app = builder.Build();

            // Enable CORS policy
            app.UseCors("AllowFrontend");

            // Serve static files from wwwroot
            app.UseStaticFiles();

            // Seed initial data (roles, admin user, sample data)
            await AppDbInitializer.SeedAsync(app);

            // Enable Swagger in development
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            // Authentication & Authorization middlewares
            app.UseAuthentication();
            app.UseAuthorization();

            // Map API controllers
            app.MapControllers();

            // Map SignalR hubs
            app.MapHub<NotificationHub>("/notificationsHub");
            app.MapHub<MessageHub>("/messagesHub");

            app.Run();
        }
    }

    // AppDbInitializer: handles seeding initial data into the database
    public static class AppDbInitializer
    {
        public static async Task SeedAsync(IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // -----------------
            // Roles
            // -----------------
            string[] roles = { "Admin", "Host", "Guest" };
            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                }
            }

            // -----------------
            // Admin user
            // -----------------
            var adminEmail = "admin@airbnbclone.com";
            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                // Create Admin user using domain factory
                admin = User.Create(
                    fullName: "System Admin",
                    role: UserRole.Admin
                );

                // Set Identity fields
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

            // -----------------
            // Sample Amenities
            // -----------------
            if (!context.Amenities.Any())
            {
                var wifi = Amenity.Create("Wi-Fi");
                var pool = Amenity.Create("Pool");
                var ac = Amenity.Create("Air Conditioning");

                context.Amenities.AddRange(wifi, pool, ac);
            }

            // -----------------
            // Sample Keywords
            // -----------------
            if (!context.Keywords.Any())
            {
                var beach = Keyword.Create("Beach");
                var luxury = Keyword.Create("Luxury");

                context.Keywords.AddRange(beach, luxury);
            }

            await context.SaveChangesAsync();

            // -----------------
            // Sample Listings
            // -----------------
            if (!context.Listings.Any())
            {
                var adminId = admin.Id;

                // Retrieve amenities & keywords from context
                var wifiAmenity = context.Amenities.First(a => a.Name == "Wi-Fi");
                var poolAmenity = context.Amenities.First(a => a.Name == "Pool");
                var acAmenity = context.Amenities.First(a => a.Name == "Air Conditioning");

                var beachKeyword = context.Keywords.First(k => k.Word == "Beach");
                var luxuryKeyword = context.Keywords.First(k => k.Word == "Luxury");

                // Listing1
                var listing1 = Listing.Create(
                    title: "Beach Villa",
                    description: "Luxury villa near the beach.",
                    pricePerNight:250,
                    location: "California",
                    latitude:34.0195,
                    longitude: -118.4912,
                    maxGuests:6,
                    userId: adminId,
                    tags: new List<string> { "beach", "villa", "luxury" },
                    createdBy: "System Admin"
                );

                listing1.Amenities.Add(wifiAmenity);
                listing1.Amenities.Add(poolAmenity);
                listing1.Keywords.Add(beachKeyword);
                listing1.Images.Add(ListingImage.Create(listing1.Id, "https://example.com/beach-villa.jpg"));

                // Listing2
                var listing2 = Listing.Create(
                    title: "City Apartment",
                    description: "Modern apartment in the city center.",
                    pricePerNight:120,
                    location: "New York",
                    latitude:40.7128,
                    longitude: -74.0060,
                    maxGuests:4,
                    userId: adminId,
                    tags: new List<string> { "city", "apartment", "modern" },
                    createdBy: "System Admin"
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
