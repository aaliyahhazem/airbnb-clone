using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DAL.Entities;

namespace DAL.Configurations
{
    public class ListingImageConfiguration : IEntityTypeConfiguration<ListingImage>
    {
        public void Configure(EntityTypeBuilder<ListingImage> builder)
        {
            builder.HasKey(li => li.Id);

            builder.Property(li => li.ImageUrl)
                   .HasMaxLength(500)
                   .IsRequired();

            // Auditing & soft-delete columns
            builder.Property(li => li.CreatedAt)
                   .HasDefaultValueSql("GETUTCDATE()")
                   .IsRequired();

            builder.Property(li => li.CreatedBy)
                   .HasMaxLength(150)
                   .IsRequired(); // must match non-nullable entity

            builder.Property(li => li.UpdatedBy)
                   .HasMaxLength(150)
                   .IsRequired(false);

            builder.Property(li => li.DeletedBy)
                   .HasMaxLength(150)
                   .IsRequired(false);

            builder.Property(li => li.IsDeleted)
                   .HasDefaultValue(false);

            // Concurrency token (RowVersion)
            builder.Property(li => li.RowVersion)
                   .IsRowVersion();

            // Relationship: Listing -> Images
            builder.HasOne(li => li.Listing)
                   .WithMany(l => l.Images)
                   .HasForeignKey(li => li.ListingId)
                   .OnDelete(DeleteBehavior.Cascade); // remove images if listing is hard-deleted

            // Index for faster image lookups, include IsDeleted for owner/admin queries
            builder.HasIndex(li => new { li.ListingId, li.IsDeleted });
        }
    }
}
