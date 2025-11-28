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
            builder.Services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile<FavoriteProfile>();
            });
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
            await PL.Helpers.Seeder.SeedIfNeededAsync(app);

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
}
