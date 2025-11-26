namespace BLL.Services.Abstractions
{
    public interface IEmailService
    {
        Task SendBookingConfirmationAsync(BookingConfirmationVM model);        // Send guest booking confirmation email
        Task SendPaymentReceiptAsync(PaymentReceiptVM model);                  // Send guest payment receipt email
        Task SendCancellationEmailAsync(CancellationEmailVM model);            // Send guest booking cancellation email
        Task SendCheckInReminderAsync(CheckInReminderEmailVM model);           // Send guest check-in reminder email
        Task SendCheckOutReminderAsync(CheckOutReminderEmailVM model);         // Send guest check-out reminder email
        Task SendHostNewBookingAsync(HostNewBookingVM model);                  // Send host new booking notification email
        Task SendPayoutNotificationAsync(PayoutNotificationVM model);          // Send host payout notification email
        Task SendPasswordResetAsync(PasswordResetEmailVM model);               // Send password reset email
    }
}
