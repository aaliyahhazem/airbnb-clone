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
<p>Hello {model.GuestName},</p>
<p>Your booking at <strong>{model.ListingTitle}</strong> is confirmed!</p>
<ul>
<li>Check-In: {FormatDate(model.CheckIn)}</li>
<li>Check-Out: {FormatDate(model.CheckOut)}</li>
<li>Total Price: {model.TotalPrice} EGP</li>
</ul>
<p>Thank you,<br/>YourAppName Team</p>";

            return SendAsync(model.Email, "Booking Confirmation", WrapHtml("Booking Confirmed!", body));
        }

        public Task SendPaymentReceiptAsync(PaymentReceiptVM model)
        {
            var body = $@"
<p>Hi {model.GuestName},</p>
<p>Your payment of <strong>{model.Amount} EGP</strong> was successful.</p>
<ul>
<li>Paid At: {model.PaidAt:yyyy-MM-dd HH:mm}</li>
<li>Method: {model.PaymentMethod}</li>
</ul>
<p>Thank you,<br/>YourAppName Team</p>";

            return SendAsync(model.Email, "Payment Receipt", WrapHtml("Payment Successful", body));
        }

        public Task SendCancellationEmailAsync(CancellationEmailVM model)
        {
            var body = $@"
<p>Hello {model.GuestName},</p>
<p>Your reservation for <strong>{model.ListingTitle}</strong> has been cancelled.</p>
<ul>
<li>Cancelled By: {(model.CancelledByHost ? "Host" : "You")}</li>
<li>Cancelled At: {model.CancelledAt:yyyy-MM-dd HH:mm}</li>
</ul>
<p>Thank you,<br/>YourAppName Team</p>";

            return SendAsync(model.Email, "Booking Cancelled", WrapHtml("Booking Cancelled", body));
        }

        public Task SendHostNewBookingAsync(HostNewBookingVM model)
        {
            var body = $@"
<p>Hello {model.HostName},</p>
<p>A new booking was made for <strong>{model.ListingTitle}</strong>.</p>
<ul>
<li>Guest: {model.GuestName}</li>
<li>Check-In: {FormatDate(model.CheckIn)}</li>
<li>Check-Out: {FormatDate(model.CheckOut)}</li>
</ul>
<p>Thank you for hosting,<br/>YourAppName Team</p>";

            return SendAsync(model.HostEmail, "New Booking on Your Listing", WrapHtml("New Booking Received", body));
        }

        public Task SendCheckInReminderAsync(CheckInReminderEmailVM model)
        {
            var body = $@"
<p>Hello {model.GuestName},</p>
<p>This is a friendly reminder that your stay at <strong>{model.ListingTitle}</strong> starts tomorrow!</p>
<ul>
<li>Check-In: {FormatDate(model.CheckInDate)}</li>
</ul>
<p>Enjoy your stay!<br/>YourAppName Team</p>";

            return SendAsync(model.Email, "Check-In Reminder", WrapHtml("Check-In Reminder", body));
        }

        public Task SendCheckOutReminderAsync(CheckOutReminderEmailVM model)
        {
            var body = $@"
<p>Hello {model.GuestName},</p>
<p>This is a friendly reminder that your stay at <strong>{model.ListingTitle}</strong> ends tomorrow.</p>
<ul>
<li>Check-Out: {FormatDate(model.CheckOutDate)}</li>
</ul>
<p>We hope you enjoyed your stay!<br/>YourAppName Team</p>";

            return SendAsync(model.Email, "Check-Out Reminder", WrapHtml("Check-Out Reminder", body));
        }

        public Task SendPayoutNotificationAsync(PayoutNotificationVM model)
        {
            var body = $@"
<p>Hello {model.HostName},</p>
<p>Your payout for <strong>{model.ListingTitle}</strong> has been processed.</p>
<ul>
<li>Amount: {model.Amount} EGP</li>
<li>Payout Date: {FormatDate(model.PayoutDate)}</li>
<li>Transaction ID: {model.TransactionId}</li>
</ul>
<p>Thank you for hosting,<br/>YourAppName Team</p>";

            return SendAsync(model.Email, "Payout Processed", WrapHtml("Payout Notification", body));
        }

        public Task SendPasswordResetAsync(PasswordResetEmailVM model)
        {
            var body = $@"
<p>Hello {model.UserName},</p>
<p>We received a request to reset your password.</p>
<p><a href='{model.ResetLink}' class='button'>Reset Password</a></p>
<p>This link expires at: {model.ExpirationTime:yyyy-MM-dd HH:mm}</p>
<p>If you did not request this, ignore this email.</p>";

            return SendAsync(model.Email, "Password Reset Request", WrapHtml("Password Reset Request", body));
        }
    }
}