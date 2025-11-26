namespace BLL.Services.Impelementation
{
    public class EmailMappingService
    {
        // Booking Confirmation
        public BookingConfirmationVM ToBookingConfirmationVM(Booking booking)
            => new BookingConfirmationVM
            {
                GuestName = booking.Guest.FullName,
                ListingTitle = booking.Listing.Title,
                CheckIn = booking.CheckInDate,
                CheckOut = booking.CheckOutDate,
                TotalPrice = booking.TotalPrice,
                Email = booking.Guest.Email!
            };

        // Payment Receipt
        public PaymentReceiptVM ToPaymentReceiptVM(Booking booking)
        {
            if (booking.Payment == null)
                throw new InvalidOperationException("Booking has no payment.");

            return new PaymentReceiptVM
            {
                GuestName = booking.Guest.FullName,
                Email = booking.Guest.Email!,
                Amount = booking.Payment.Amount,
                PaidAt = booking.Payment.PaidAt,
                PaymentMethod = booking.Payment.PaymentMethod
            };
        }

        // Cancellation Email
        public CancellationEmailVM ToCancellationVM(Booking booking, bool cancelledByHost)
            => new CancellationEmailVM
            {
                GuestName = booking.Guest.FullName,
                Email = booking.Guest.Email!,
                ListingTitle = booking.Listing.Title,
                CheckIn = booking.CheckInDate,
                CancelledByHost = cancelledByHost,
                CancelledAt = DateTime.UtcNow
            };

        // Check-In Reminder
        public CheckInReminderEmailVM ToCheckInReminderVM(Booking booking)
            => new CheckInReminderEmailVM
            {
                Email = booking.Guest.Email!,
                GuestName = booking.Guest.FullName,
                ListingTitle = booking.Listing.Title,
                CheckInDate = booking.CheckInDate
            };

        // Check-Out Reminder
        public CheckOutReminderEmailVM ToCheckOutReminderVM(Booking booking)
            => new CheckOutReminderEmailVM
            {
                Email = booking.Guest.Email!,
                GuestName = booking.Guest.FullName,
                ListingTitle = booking.Listing.Title,
                CheckOutDate = booking.CheckOutDate
            };

        // Host New Booking
        public HostNewBookingVM ToHostNewBookingVM(Booking booking)
            => new HostNewBookingVM
            {
                HostName = booking.Listing.User.FullName,
                HostEmail = booking.Listing.User.Email!,
                GuestName = booking.Guest.FullName,
                ListingTitle = booking.Listing.Title,
                CheckIn = booking.CheckInDate,
                CheckOut = booking.CheckOutDate
            };

        // Payout Notification (Host)
        public PayoutNotificationVM ToPayoutNotificationVM(Booking booking)
        {
            if (booking.Payment == null)
                throw new InvalidOperationException("Booking has no payment.");

            return new PayoutNotificationVM
            {
                Email = booking.Listing.User.Email!,
                HostName = booking.Listing.User.FullName,
                ListingTitle = booking.Listing.Title,
                Amount = booking.Payment.Amount,
                PayoutDate = DateTime.UtcNow,
                TransactionId = booking.Payment.TransactionId
            };
        }

        // Password Reset
        public PasswordResetEmailVM ToPasswordResetVM(User user, string resetLink, DateTime expiresAt)
            => new PasswordResetEmailVM
            {
                Email = user.Email!,
                UserName = user.FullName,
                ResetLink = resetLink,
                ExpirationTime = expiresAt
            };
    }
}
