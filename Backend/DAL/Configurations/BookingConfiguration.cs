namespace DAL.Configurations
{
    public class BookingConfiguration : IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> builder)
        {
            builder.HasKey(b => b.Id);

            builder.Property(b => b.TotalPrice)
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(b => b.CreatedAt)
                   .HasDefaultValueSql("GETUTCDATE()")
                   .IsRequired();

            builder.Property(b => b.PaymentStatus)
                   .HasConversion<string>()
                   .HasMaxLength(20)
                   .HasDefaultValue(BookingPaymentStatus.Pending)
                   .IsRequired();

            builder.Property(b => b.BookingStatus)
                   .HasConversion<string>()
                   .HasMaxLength(20)
                   .HasDefaultValue(BookingStatus.Pending)
                   .IsRequired();

            // Relationships
            builder.HasOne(b => b.Listing)
                   .WithMany(l => l.Bookings)
                   .HasForeignKey(b => b.ListingId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(b => b.Guest)
                   .WithMany(u => u.Bookings)
                   .HasForeignKey(b => b.GuestId)
                   .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(b => b.Payment)
                   .WithOne(p => p.Booking)
                   .HasForeignKey<Payment>(p => p.BookingId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(b => b.Review)
                   .WithOne(r => r.Booking)
                   .HasForeignKey<Review>(r => r.BookingId)
                   .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
