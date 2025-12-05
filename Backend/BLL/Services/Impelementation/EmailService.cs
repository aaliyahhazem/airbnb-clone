namespace BLL.Services.Impelementation
{
    public class EmailService : IEmailService
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPass;
        private readonly string _fromEmail;

        public EmailService(IConfiguration config)
        {
            _smtpServer = config["Email:SmtpServer"]!;
            _smtpPort = int.Parse(config["Email:SmtpPort"]!);
            _smtpUser = config["Email:SmtpUser"]!;
            _smtpPass = config["Email:SmtpPass"]!;
            _fromEmail = config["Email:FromEmail"]!;
        }

        // Central send method (HTML)
        private async Task SendAsync(string to, string subject, string htmlBody)
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_fromEmail));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync(_smtpServer, _smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_smtpUser, _smtpPass);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        // Helpers
        private string WrapHtml(string title, string body)
        {
            return $@"
                <html>
                <head>
                <style>
                    body {{ font-family: Arial, sans-serif; background-color:#f7f7f7; color:#333; }}
                    .container {{ max-width:600px; margin:auto; background:white; padding:20px; border-radius:10px; }}
                    h2 {{ color:#2a7ae2; }}
                    a.button {{ display:inline-block; background:#2a7ae2; color:white; padding:10px 20px; text-decoration:none; border-radius:5px; }}
                </style>
                </head>
                <body>
                <div class='container'>
                <h2>{title}</h2>
                {body}
                </div>
                </body>
                </html>";
        }

        private string FormatDate(DateTime date) => date.ToString("yyyy-MM-dd");

        // Emails

        public Task SendBookingConfirmationAsync(BookingConfirmationVM model)
        {
            var body = $@"
        <p>Hi <strong>{model.GuestName}</strong>,</p>
        <p>We’re excited to confirm your booking at <strong>{model.ListingTitle}</strong>!</p>
        <table style='width:100%; border-collapse: collapse;'>
            <tr><td style='padding:5px; font-weight:bold;'>Check-In:</td><td>{FormatDate(model.CheckIn)}</td></tr>
            <tr><td style='padding:5px; font-weight:bold;'>Check-Out:</td><td>{FormatDate(model.CheckOut)}</td></tr>
            <tr><td style='padding:5px; font-weight:bold;'>Total Price:</td><td>{model.TotalPrice} EGP</td></tr>
        </table>
        <p>Please check your booking details in your account. We wish you a wonderful stay!</p>
        <p>Thank you,<br/><strong>The Broker Team</strong></p>";

            return SendAsync(model.Email, "Your Booking is Confirmed!", WrapHtml("Booking Confirmed!", body));
        }

        public Task SendPaymentReceiptAsync(PaymentReceiptVM model)
        {
            var body = $@"
        <p>Hi <strong>{model.GuestName}</strong>,</p>
        <p>We have received your payment successfully.</p>
        <table style='width:100%; border-collapse: collapse;'>
            <tr><td style='padding:5px; font-weight:bold;'>Amount Paid:</td><td>{model.Amount} EGP</td></tr>
            <tr><td style='padding:5px; font-weight:bold;'>Payment Method:</td><td>{model.PaymentMethod}</td></tr>
            <tr><td style='padding:5px; font-weight:bold;'>Paid At:</td><td>{model.PaidAt:yyyy-MM-dd HH:mm}</td></tr>
        </table>
        <p>Your booking is now fully secured. We look forward to hosting you!</p>
        <p>Thank you,<br/><strong>The Broker Team</strong></p>";

            return SendAsync(model.Email, "Payment Receipt", WrapHtml("Payment Successful", body));
        }

        public Task SendHostNewBookingAsync(HostNewBookingVM model)
        {
            var body = $@"
        <p>Hi <strong>{model.HostName}</strong>,</p>
        <p>You have a new booking for your listing: <strong>{model.ListingTitle}</strong>.</p>
        <table style='width:100%; border-collapse: collapse;'>
            <tr><td style='padding:5px; font-weight:bold;'>Guest:</td><td>{model.GuestName}</td></tr>
            <tr><td style='padding:5px; font-weight:bold;'>Check-In:</td><td>{FormatDate(model.CheckIn)}</td></tr>
            <tr><td style='padding:5px; font-weight:bold;'>Check-Out:</td><td>{FormatDate(model.CheckOut)}</td></tr>
        </table>
        <p>Check your dashboard for more details and to manage your bookings.</p>
        <p>Thank you for hosting,<br/><strong>The Broker Team</strong></p>";

            return SendAsync(model.HostEmail, "New Booking on Your Listing", WrapHtml("New Booking Received", body));
        }

        public Task SendPayoutNotificationAsync(PayoutNotificationVM model)
        {
            var body = $@"
        <p>Hi <strong>{model.HostName}</strong>,</p>
        <p>We’ve processed a payout for your listing <strong>{model.ListingTitle}</strong>.</p>
        <table style='width:100%; border-collapse: collapse;'>
            <tr><td style='padding:5px; font-weight:bold;'>Amount:</td><td>{model.Amount} EGP</td></tr>
            <tr><td style='padding:5px; font-weight:bold;'>Payout Date:</td><td>{FormatDate(model.PayoutDate)}</td></tr>
            <tr><td style='padding:5px; font-weight:bold;'>Transaction ID:</td><td>{model.TransactionId}</td></tr>
        </table>
        <p>The amount should appear in your bank account shortly. Check your payout history in your dashboard for details.</p>
        <p>Thank you for hosting,<br/><strong>The Broker Team</strong></p>";

            return SendAsync(model.Email, "Payout Processed", WrapHtml("Payout Notification", body));
        }

        public Task SendCancellationEmailAsync(CancellationEmailVM model)
        {
            var body = $@"
        <p>Hi <strong>{model.GuestName}</strong>,</p>
        <p>Your reservation for <strong>{model.ListingTitle}</strong> has been cancelled.</p>
        <table style='width:100%; border-collapse: collapse;'>
            <tr><td style='padding:5px; font-weight:bold;'>Cancelled By:</td><td>{(model.CancelledByHost ? "Host" : "You")}</td></tr>
            <tr><td style='padding:5px; font-weight:bold;'>Cancelled At:</td><td>{model.CancelledAt:yyyy-MM-dd HH:mm}</td></tr>
        </table>
        <p>If you have any questions, please contact support.</p>
        <p>Thank you,<br/><strong>The Broker Team</strong></p>";

            return SendAsync(model.Email, "Booking Cancelled", WrapHtml("Booking Cancelled", body));
        }

        public Task SendCheckInReminderAsync(CheckInReminderEmailVM model)
        {
            var body = $@"
        <p>Hi <strong>{model.GuestName}</strong>,</p>
        <p>This is a friendly reminder that your stay at <strong>{model.ListingTitle}</strong> begins tomorrow!</p>
        <table style='width:100%; border-collapse: collapse; margin-top:10px;'>
            <tr><td style='padding:5px; font-weight:bold;'>Check-In Date:</td><td>{FormatDate(model.CheckInDate)}</td></tr>
        </table>
        <p>Please make sure you have all your travel details ready. We hope you have a wonderful stay!</p>
        <p>Thank you,<br/><strong>The Broker Team</strong></p>";

            return SendAsync(model.Email, "Reminder: Your Stay Starts Tomorrow", WrapHtml("Check-In Reminder", body));
        }

        public Task SendCheckOutReminderAsync(CheckOutReminderEmailVM model)
        {
            var body = $@"
        <p>Hi <strong>{model.GuestName}</strong>,</p>
        <p>This is a friendly reminder that your stay at <strong>{model.ListingTitle}</strong> ends tomorrow.</p>
        <table style='width:100%; border-collapse: collapse; margin-top:10px;'>
            <tr><td style='padding:5px; font-weight:bold;'>Check-Out Date:</td><td>{FormatDate(model.CheckOutDate)}</td></tr>
        </table>
        <p>We hope you enjoyed your stay! Don’t forget to leave a review and share your experience.</p>
        <p>Thank you,<br/><strong>The Broker Team</strong></p>";

            return SendAsync(model.Email, "Reminder: Your Stay Ends Tomorrow", WrapHtml("Check-Out Reminder", body));
        }

        public Task SendPasswordResetAsync(PasswordResetEmailVM model)
        {
            var body = $@"
        <p>Hi <strong>{model.UserName}</strong>,</p>
        <p>We received a request to reset your password for your account.</p>
        <p style='text-align:center; margin:20px 0;'>
            <a href='{model.ResetLink}' 
               style='display:inline-block; background-color:#2a7ae2; color:white; padding:12px 24px; text-decoration:none; border-radius:6px; font-weight:bold;'>
               Reset Password
            </a>
        </p>
        <p>This link will expire on: <strong>{model.ExpirationTime:yyyy-MM-dd HH:mm}</strong></p>
        <p>If you did not request a password reset, please ignore this email.</p>
        <p>Thank you,<br/><strong>The Broker Team</strong></p>";

            return SendAsync(model.Email, "Reset Your Password", WrapHtml("Password Reset Request", body));
        }
    }
}