namespace DAL.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Id)
                   .ValueGeneratedOnAdd();

            // Extra properties
            builder.Property(u => u.FullName)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(u => u.Role)
                   .HasConversion<string>()
                   .HasMaxLength(20)
                   .HasDefaultValue(UserRole.Guest)
                   .IsRequired();

            builder.Property(u => u.ProfileImg)
                   .HasMaxLength(250)
                   .IsRequired(false);

            builder.Property(u => u.DateCreated)
                   .HasDefaultValueSql("GETUTCDATE()")
                   .IsRequired();

            builder.Property(u => u.FirebaseUid)
                   .HasMaxLength(100)
                   .IsRequired(false);

            builder.HasIndex(u => u.FirebaseUid)
                   .IsUnique();

            builder.Property(u => u.IsActive)
                   .HasDefaultValue(true)
                   .IsRequired();

            // Identity properties
            builder.Property(u => u.Email)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.HasIndex(u => u.Email)
                   .IsUnique();

            // Configure UserName column and unique index
            builder.Property(u => u.UserName)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.HasIndex(u => u.UserName)
                   .IsUnique();

            builder.Property(u => u.PhoneNumber)
                   .HasMaxLength(20)
                   .IsRequired(false);

            builder.Property(u => u.PasswordHash)
                   .IsRequired();

            // Relationships
            builder.HasMany(u => u.Listings)
                   .WithOne(l => l.User)
                   .HasForeignKey(l => l.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.Bookings)
                   .WithOne(b => b.Guest)
                   .HasForeignKey(b => b.GuestId)
                   .OnDelete(DeleteBehavior.NoAction);

            builder.HasMany(u => u.MessagesSent)
                   .WithOne(m => m.Sender)
                   .HasForeignKey(m => m.SenderId)
                   .OnDelete(DeleteBehavior.NoAction);

            builder.HasMany(u => u.MessagesReceived)
                   .WithOne(m => m.Receiver)
                   .HasForeignKey(m => m.ReceiverId)
                   .OnDelete(DeleteBehavior.NoAction);

            builder.HasMany(u => u.Reviews)
                   .WithOne(r => r.Guest)
                   .HasForeignKey(r => r.GuestId)
                   .OnDelete(DeleteBehavior.NoAction);


            builder.HasMany(u => u.Notifications)
                   .WithOne(n => n.User)
                   .HasForeignKey(n => n.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}