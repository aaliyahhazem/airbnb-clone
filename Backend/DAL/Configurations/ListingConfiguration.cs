using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;


namespace DAL.Configurations
{
    public class ListingConfiguration : IEntityTypeConfiguration<Listing>
    {
        public void Configure(EntityTypeBuilder<Listing> builder)
        {
            builder.HasKey(l => l.Id);

            // Table-level check constraints (SQL Server style)
            builder.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Listing_Price", "[PricePerNight] > 0");
                t.HasCheckConstraint("CK_Listing_Latitude", "[Latitude] >= -90 AND [Latitude] <= 90");
                t.HasCheckConstraint("CK_Listing_Longitude", "[Longitude] >= -180 AND [Longitude] <= 180");
                t.HasCheckConstraint("CK_Listing_MaxGuests", "[MaxGuests] > 0");
            });

            // Main fields
            builder.Property(l => l.Title)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(l => l.Description)
                .HasMaxLength(2000)
                .IsRequired();

            builder.Property(l => l.Location)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(l => l.PricePerNight)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(l => l.Destination)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(l => l.Type)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(l => l.NumberOfRooms)
                .IsRequired();

            builder.Property(l => l.NumberOfBathrooms)
                .IsRequired();

            builder.Property(l => l.Latitude)
                .HasColumnType("float");

            builder.Property(l => l.Longitude)
                .HasColumnType("float");

            builder.Property(l => l.MaxGuests)
                .IsRequired();

            builder.Property(l => l.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()")
                .IsRequired();

            builder.Property(l => l.IsPromoted)
                .HasDefaultValue(false);

            builder.Property(l => l.PromotionEndDate)
                .IsRequired(false);

         
            // Auditing
            builder.Property(l => l.CreatedBy)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(l => l.UpdatedBy)
                .HasMaxLength(150)
                .IsRequired(false);

            builder.Property(l => l.DeletedBy)
                .HasMaxLength(150)
                .IsRequired(false);

            builder.Property(l => l.IsDeleted)
                .HasDefaultValue(false);

            builder.Property(l => l.IsReviewed)
                .HasDefaultValue(false);

            builder.Property(l => l.IsApproved)
                .HasDefaultValue(false);

            // Relationships
            builder.HasOne(l => l.User)
                .WithMany(u => u.Listings)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(l => l.Bookings)
                .WithOne(b => b.Listing)
                .HasForeignKey(b => b.ListingId)
                .OnDelete(DeleteBehavior.Cascade);


            // optional explicit main image FK
            builder.HasOne(l => l.MainImage)
                    .WithMany()
                    .HasForeignKey(l => l.MainImageId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(l => l.Amenities)
                    .WithOne(k => k.Listing)
                    .HasForeignKey(k => k.ListingId)
                    .OnDelete(DeleteBehavior.Cascade);
            // Favorites relationship
            builder.HasMany(l => l.Favorites)
                 .WithOne(f => f.Listing)
                 .HasForeignKey(f => f.ListingId)
                 .OnDelete(DeleteBehavior.NoAction); // Avoid multiple cascade paths
            // Indexes
            builder.HasIndex(l => l.Location);
            builder.HasIndex(l => l.PricePerNight);
            builder.HasIndex(l => l.CreatedAt);
            builder.HasIndex(l => l.IsPromoted);
            builder.HasIndex(l => l.UserId);
            builder.HasIndex(l => new { l.UserId, l.IsDeleted });
        }
    }
}
