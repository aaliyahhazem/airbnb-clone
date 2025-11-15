using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using DAL.Entities;
using System.Collections.Generic;
using System.Linq;

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

            // Tags JSON conversion + ValueComparer so EF detects list changes
            var tagsComparer = new ValueComparer<List<string>>(
                (a, b) => a.SequenceEqual(b),
                a => a.Aggregate(0, (h, s) => HashCode.Combine(h, s == null ? 0 : s.GetHashCode())),
                a => a.ToList()
            );

            builder.Property(l => l.Tags)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                    v => string.IsNullOrEmpty(v) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(v, new JsonSerializerOptions())!
                )
                .HasColumnType("nvarchar(max)")
                .IsRequired(false)
                .Metadata.SetValueComparer(tagsComparer);

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

            //builder.Property(l => l.Priority)
            //    .HasDefaultValue(0);

            // Concurrency token (RowVersion)
            builder.Property(l => l.RowVersion)
                .IsRowVersion();
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
