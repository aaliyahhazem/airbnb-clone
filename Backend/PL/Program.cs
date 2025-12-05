using Hangfire;
using Hangfire.SqlServer;
using PL.Background_Jobs;

using Microsoft.Extensions.FileProviders;

namespace PL
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Enable detailed logging for development
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();
            
            if (builder.Environment.IsDevelopment())
            {
                builder.Logging.SetMinimumLevel(LogLevel.Information);
                builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information);
            }

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

            //--------------------------------------------------------------------
            //Background Jobs
            //---------------------------------------------------------------------
            // Hangfire storage
            builder.Services.AddHangfire(config =>
            {
                config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                      .UseSimpleAssemblyNameTypeSerializer()
                      .UseRecommendedSerializerSettings()
                      .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"),
                          new SqlServerStorageOptions
                          {
                              CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                              SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                              QueuePollInterval = TimeSpan.FromSeconds(15),
                              UseRecommendedIsolationLevel = true,
                              DisableGlobalLocks = true
                          });
            });
            builder.Services.AddHangfireServer();
            // Register jobs
            builder.Services.AddTransient<MessageCleanupJob>();
            builder.Services.AddTransient<NotificationCleanupJob>();
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

            // Firebase Admin SDK initialization
            try
            {
                var firebaseConfig = builder.Configuration.GetSection("Firebase");
                var serviceAccount = firebaseConfig.GetSection("ServiceAccount");

                var credentialsJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    type = serviceAccount["type"],
                    project_id = serviceAccount["project_id"],
                    private_key_id = serviceAccount["private_key_id"],
                    private_key = serviceAccount["private_key"],
                    client_email = serviceAccount["client_email"],
                    client_id = serviceAccount["client_id"],
                    auth_uri = serviceAccount["auth_uri"],
                    token_uri = serviceAccount["token_uri"],
                    auth_provider_x509_cert_url = serviceAccount["auth_provider_x509_cert_url"],
                    client_x509_cert_url = serviceAccount["client_x509_cert_url"]
                });

                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromJson(credentialsJson),
                    ProjectId = firebaseConfig["ProjectId"]
                });

                Console.WriteLine("Firebase Admin SDK initialized successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Firebase initialization failed: {ex.Message}");
            }

            // Email reminder related service
            builder.Services.AddHostedService<BookingReminderService>();

            // --------------------------------------------------------------------
            // DAL/BLL registrations
            // --------------------------------------------------------------------
            builder.Services.AddBuissinesInDAL();
            builder.Services.AddBuissinesInBLL();

            // Make sure AdminRepository exists
            builder.Services.AddScoped<DAL.Repo.Abstraction.IAdminRepository, DAL.Repo.Implementation.AdminRepository>();

            // AutoMapper – scan all assemblies for profiles
            //builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            builder.Services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile<FavoriteProfile>();
            });

            // --------------------------------------------------------------------
            // Localization
            // --------------------------------------------------------------------
            builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

            builder.Services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[] { "en-US", "ar" };
                options.SetDefaultCulture("en-US")
                    .AddSupportedCultures(supportedCultures)
                    .AddSupportedUICultures(supportedCultures);

                // Culture providers priority:
                // 1. Query string parameter (lang)
                // 2. Cookie
                // 3. Accept-Language header
                options.RequestCultureProviders = new List<IRequestCultureProvider>
                {
                    new QueryStringRequestCultureProvider { QueryStringKey = "lang" },
                    new CookieRequestCultureProvider { CookieName = "app_language" },
                    new AcceptLanguageHeaderRequestCultureProvider()
                };
            });
            
            // Controllers & SignalR
            builder.Services.AddControllers();
            builder.Services.AddSignalR();

            // Notification publisher (sends SignalR messages from BLL)
            builder.Services.AddSingleton<BLL.Services.Abstractions.INotificationPublisher, PL.Services.NotificationPublisher>();

            // Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var modelPath = builder.Configuration["FaceModelsPath"];
            builder.Services.AddSingleton(provider =>
                FaceRecognitionDotNet.FaceRecognition.Create(modelPath));

            var app = builder.Build();

            // --------------------------------------------------------------------
            // Middleware pipeline
            // --------------------------------------------------------------------
            app.UseCors("AllowFrontend");

            // Enable Request Localization
            app.UseRequestLocalization();

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
            // This tells .NET: "If a request comes in starting with /Files, 
            // look inside the physical 'Files' folder in the project root."
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(builder.Environment.ContentRootPath, "Files")),
                RequestPath = "/Files"
            });

            // Hangfire Dashboard
            app.UseHangfireDashboard("/hangfire");

            // register recurring jobs reading cron from config
            var messageCron = builder.Configuration.GetValue<string>("BackgroundJobs:MessageCleanupCron", Cron.Daily());
            var notifCron = builder.Configuration.GetValue<string>("BackgroundJobs:NotificationCleanupCron", Cron.Daily());

            RecurringJob.AddOrUpdate<MessageCleanupJob>(
                "cleanup-messages",
                job => job.ExecuteAsync(),
                messageCron
            );

            RecurringJob.AddOrUpdate<NotificationCleanupJob>(
                "cleanup-notifications",
                job => job.ExecuteAsync(),
                notifCron
            );
            app.MapControllers();

            app.MapHub<NotificationHub>("/notificationsHub");
            app.MapHub<MessageHub>("/messagesHub");

            app.Run();
        }
    }
}
