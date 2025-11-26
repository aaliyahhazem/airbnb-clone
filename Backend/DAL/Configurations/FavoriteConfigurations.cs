using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Configurations
{
    public class FavoriteConfigurations : IEntityTypeConfiguration<Favorite>
    {
        public void Configure(EntityTypeBuilder<Favorite> builder)
        {
            builder.ToTable("Favorites");

            builder.HasKey(f => f.Id);

            // Properties
            builder.Property(f => f.UserId)
                   .IsRequired();

            builder.Property(f => f.ListingId)
                   .IsRequired();

            builder.Property(f => f.CreatedAt)
                   .HasDefaultValueSql("GETUTCDATE()")
                   .IsRequired();

            // Relationships
            builder.HasOne(f => f.User)
                   .WithMany(u => u.Favorites)
                   .HasForeignKey(f => f.UserId)
                   .OnDelete(DeleteBehavior.NoAction); // Prevent multiple cascade paths

            builder.HasOne(f => f.Listing)
                   .WithMany(l => l.Favorites)
                   .HasForeignKey(f => f.ListingId)
                   .OnDelete(DeleteBehavior.NoAction); // Prevent multiple cascade paths
            // Indexes for performance
            builder.HasIndex(f => f.UserId);
            builder.HasIndex(f => f.ListingId);

            // Unique constraint
            builder.HasIndex(f => new { f.UserId, f.ListingId })
                   .IsUnique();
        }
    }
}
