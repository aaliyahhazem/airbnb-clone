public class AmenityConfiguration : IEntityTypeConfiguration<Amenity>
{
    public void Configure(EntityTypeBuilder<Amenity> builder)
    {
        builder.HasKey(k => k.Id);

        builder.Property(k => k.Word)
               .IsRequired()
               .HasMaxLength(200);

        // One-to-many: each Amenity belongs to one Listing
        builder.HasOne(k => k.Listing)
               .WithMany(l => l.Amenities)
               .HasForeignKey(k => k.ListingId)
               .IsRequired()
               .OnDelete(DeleteBehavior.Cascade);

        // Optional: index for faster lookups by ListingId
        builder.HasIndex(k => k.ListingId);
    }
}
