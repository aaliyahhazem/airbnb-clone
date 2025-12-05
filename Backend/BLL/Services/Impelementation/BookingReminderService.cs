using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BLL.Services.Impelementation
{
    public class BookingReminderService : BackgroundService
    {
        private readonly ILogger<BookingReminderService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public BookingReminderService(
            ILogger<BookingReminderService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Booking Reminder Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                        var emailMapper = scope.ServiceProvider.GetRequiredService<EmailMappingService>();
                        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                        await SendRemindersAsync(uow, emailMapper, emailService);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while sending booking reminders.");
                }

                // Run every 1 hour
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task SendRemindersAsync(
            IUnitOfWork _uow,
            EmailMappingService _emailMappingService,
            IEmailService _emailService)
        {
            var today = DateTime.UtcNow.Date;

            // --- Check-in reminders (tomorrow) ---
            var checkInBookings = await _uow.Bookings
                .GetBookingsForCheckInReminderAsync(today.AddDays(1));

            foreach (var booking in checkInBookings)
            {
                try
                {
                    var vm = _emailMappingService.ToCheckInReminderVM(booking);
                    await _emailService.SendCheckInReminderAsync(vm);

                    _logger.LogInformation("Check-in reminder sent for bookingId={BookingId}", booking.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed sending check-in reminder for bookingId={BookingId}", booking.Id);
                }
            }

            // --- Check-out reminders (tomorrow) ---
            var checkOutBookings = await _uow.Bookings
                .GetBookingsForCheckOutReminderAsync(today.AddDays(1));

            foreach (var booking in checkOutBookings)
            {
                try
                {
                    var vm = _emailMappingService.ToCheckOutReminderVM(booking);
                    await _emailService.SendCheckOutReminderAsync(vm);

                    _logger.LogInformation("Check-out reminder sent for bookingId={BookingId}", booking.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed sending check-out reminder for bookingId={BookingId}", booking.Id);
                }
            }
        }
    }
}
