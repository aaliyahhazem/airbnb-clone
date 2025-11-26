
namespace DAL.Database
{
    public class AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Listing> Listings { get; set; } = null!;
        public DbSet<Booking> Bookings { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<Review> Reviews { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<ListingImage> ListingImages { get; set; } = null!;
        public DbSet<Amenity> Amenities { get; set; } = null!;
        public DbSet<Favorite> Favorites { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Rename identity tables
            builder.Entity<User>().ToTable("Users");
            builder.Entity<IdentityRole<Guid>>().ToTable("Roles");
            builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
            builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
            builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
            builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
            builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
            builder.Entity<Listing>().HasQueryFilter(l => !l.IsDeleted);
            builder.Entity<ListingImage>().HasQueryFilter(li => !li.IsDeleted);
            // Apply all entity configurations
            builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}