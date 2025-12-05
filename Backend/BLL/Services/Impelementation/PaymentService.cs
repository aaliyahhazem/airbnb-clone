using Microsoft.Extensions.Logging;

namespace BLL.Services.Impelementation
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _uow;
        private readonly INotificationService _notificationService;
        private readonly IOptions<StripeSettings> _stripeSettings;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(IUnitOfWork uow,
                              INotificationService notificationService,
                              IOptions<StripeSettings> stripeSettings,
                              ILogger<PaymentService> logger)
        {
            _uow = uow;
            _notificationService = notificationService;
            _stripeSettings = stripeSettings;
            _logger = logger;
            // Configure Stripe API key
            StripeConfiguration.ApiKey = _stripeSettings.Value.SecretKey;
        }

        // Initiate payment: create Payment record with Pending status; in real app call Stripe/PayPal
        public async Task<Response<CreatePaymentVM>> InitiatePaymentAsync(Guid userId, int bookingId, decimal amount, string method)
        {
            try
            {
                var booking = await _uow.Bookings.GetByIdAsync(bookingId);
                if (booking == null) return Response<CreatePaymentVM>.FailResponse("Booking not found");

                var transactionId = Guid.NewGuid().ToString();
                var payment = Payment.Create(bookingId, amount, method, transactionId, PaymentStatus.Pending, DateTime.UtcNow);
                await _uow.Payments.AddAsync(payment);
                await _uow.SaveChangesAsync();

                // attach payment to booking (optional linking in EF tracked entities)
                booking.Update(booking.CheckInDate, booking.CheckOutDate, booking.TotalPrice, BookingPaymentStatus.Pending, booking.BookingStatus);
                _uow.Bookings.Update(booking);
                await _uow.SaveChangesAsync();

                // send notification to guest
                await _notificationService.CreateAsync(new BLL.ModelVM.Notification.CreateNotificationVM
                {
                    UserId = booking.GuestId,
                    Title = "Payment Initiated",
                    Body = $"Payment of {amount:C} initiated for your booking.",
                    CreatedAt = DateTime.UtcNow,
                    ActionUrl = $"/payment/{booking.Id}",
                    ActionLabel = "View Payment"
                });

                var vm = new CreatePaymentVM { BookingId = bookingId, Amount = amount, PaymentMethod = method };
                return Response<CreatePaymentVM>.SuccessResponse(vm);
            }
            catch (Exception ex)
            {
                return Response<CreatePaymentVM>.FailResponse(ex.Message);
            }
        }

        // Confirm payment - mark payment success
        public async Task<Response<bool>> ConfirmPaymentAsync(int bookingId, string transactionId)
        {
            try
            {
                var payments = (await _uow.Payments.GetPaymentsByBookingAsync(bookingId)).ToList();
                var payment = payments.FirstOrDefault(p => p.TransactionId == transactionId);
                if (payment == null) return Response<bool>.FailResponse("Payment not found");

                payment.Update(payment.Amount, payment.PaymentMethod, payment.TransactionId, PaymentStatus.Success, DateTime.UtcNow);
                _uow.Payments.Update(payment);

                var booking = await _uow.Bookings.GetByIdAsync(bookingId);
                booking.Update(booking.CheckInDate, booking.CheckOutDate, booking.TotalPrice, BookingPaymentStatus.Paid, BookingStatus.Confirmed);
                _uow.Bookings.Update(booking);

                await _uow.SaveChangesAsync();

                // notify guest & host
                await _notificationService.CreateAsync(new CreateNotificationVM
                {
                    UserId = booking.GuestId,
                    Title = "Payment Confirmed",
                    Body = $"Your payment was successful! Enjoy your stay.",
                    CreatedAt = DateTime.UtcNow,
                    ActionUrl = $"/booking",
                    ActionLabel = "View Booking"
                });
                await _notificationService.CreateAsync(new CreateNotificationVM
                {
                    UserId = booking.Listing.UserId,
                    Title = "New Booking Confirmed",
                    Body = $"You have a new booking for {booking.Listing.Title}. Check-in: {booking.CheckInDate:MMM dd}",
                    CreatedAt = DateTime.UtcNow,
                    ActionUrl = $"/listings/{booking.Listing.Id}",
                    ActionLabel = "View Listing"
                });

                return Response<bool>.SuccessResponse(true);
            }
            catch (Exception ex)
            {
                return Response<bool>.FailResponse(ex.Message);
            }
        }

        // Refund payment - set status to Refunded
        public async Task<Response<bool>> RefundPaymentAsync(int paymentId)
        {
            try
            {
                var payment = await _uow.Payments.GetByIdAsync(paymentId);
                if (payment == null) return Response<bool>.FailResponse("Payment not found");

                payment.Update(payment.Amount, payment.PaymentMethod, payment.TransactionId, PaymentStatus.Refunded, DateTime.UtcNow);
                _uow.Payments.Update(payment);
                await _uow.SaveChangesAsync();

                // notify user
                var booking = await _uow.Bookings.GetByIdAsync(payment.BookingId);
                await _notificationService.CreateAsync(new CreateNotificationVM
                {
                    UserId = booking.GuestId,
                    Title = "Payment Refunded",
                    Body = $"Your refund has been processed successfully.",
                    CreatedAt = DateTime.UtcNow,
                    ActionUrl = $"/booking",
                    ActionLabel = "View Bookings"
                });

                return Response<bool>.SuccessResponse(true);
            }
            catch (Exception ex)
            {
                return Response<bool>.FailResponse(ex.Message);
            }
        }


        #region Sripe--Integration
        public async Task<Response<CreatePaymentIntentVm>> CreateStripePaymentIntentAsync(Guid userId, CreateStripePaymentVM model)
        {
            try
            {
            try { _logger?.LogInformation("CreateStripePaymentIntentAsync requested by {UserId} for booking {BookingId}, amount {Amount}", userId, model.BookingId, model.Amount); } catch {}
                var booking = await _uow.Bookings.GetByIdAsync(model.BookingId);
                if (booking == null)
                    return Response<CreatePaymentIntentVm>.FailResponse("Booking not found");
                if (booking.GuestId != userId)
                    return Response<CreatePaymentIntentVm>.FailResponse("Unauthorized");
                var existingPayment = (await _uow.Payments.GetPaymentsByBookingAsync(model.BookingId))
                                                          .FirstOrDefault(p => p.TransactionId != null);

                if (existingPayment != null)
                {
                    // Payment already exists, return existing PaymentIntent
                    if (!string.IsNullOrEmpty(booking.PaymentIntentId))
                    {

                        var existingIntent = await new PaymentIntentService()
                            .GetAsync(booking.PaymentIntentId);

                        return Response<CreatePaymentIntentVm>.SuccessResponse(new CreatePaymentIntentVm
                        {
                            ClientSecret = existingIntent.ClientSecret,
                            PaymentIntentId = existingIntent.Id,
                            Amount = existingPayment.Amount,
                            Currency = "egp" // Default or store in payment
                        });
                    }
                }

                // Validate amount to avoid creating 0-valued PaymentIntents
                if (model.Amount <= 0)
                {
                    return Response<CreatePaymentIntentVm>.FailResponse("Invalid amount");
                }

                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(model.Amount * 100), // Convert to cents
                    Currency = model.Currency.ToLower(),
                    Description = model.Description ?? $"Payment for booking #{booking.Id}",
                    Metadata = new Dictionary<string, string>
                    {
                        { "booking_id", booking.Id.ToString() },
                        { "user_id", userId.ToString() }
                    },
                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true
                    }
                };
                var service = new PaymentIntentService();
                var paymentIntent = await service.CreateAsync(options);
                var payment = Payment.Create(
                    booking.Id,
                    model.Amount,
                    "Stripe",
                    paymentIntent.Id,
                    PaymentStatus.Pending,
                    DateTime.UtcNow
                );
                await _uow.Payments.AddAsync(payment);
                booking.SetPaymentIntentId(paymentIntent.Id);
                _uow.Bookings.Update(booking);
                await _uow.SaveChangesAsync();

                await _notificationService.CreateAsync(new CreateNotificationVM
                {
                    UserId = booking.GuestId,
                    Title = "Stripe Payment Initiated",
                    Body = $"A Stripe payment of {model.Amount:C} has been initiated for booking {booking.Id}.",
                    CreatedAt = DateTime.UtcNow
                });
                var result = new CreatePaymentIntentVm
                {
                    ClientSecret = paymentIntent.ClientSecret,
                    PaymentIntentId = paymentIntent.Id,
                    Amount = model.Amount,
                    Currency = model.Currency
                };
                return Response<CreatePaymentIntentVm>.SuccessResponse(result);
            }
            catch (StripeException ex)
            {
                try { _logger?.LogWarning("Stripe error creating intent: {Message}", ex.Message); } catch {}
                return Response<CreatePaymentIntentVm>.FailResponse($"Stripe error: {ex.Message}");
            }
            catch (Exception ex)
            {
                try { _logger?.LogError(ex, "CreateStripePaymentIntentAsync general error"); } catch {}
                return Response<CreatePaymentIntentVm>.FailResponse(ex.Message);
            }
        }

        public async Task<Response<bool>> HandleStripeWebhookAsync(string payload, string signature)
        {
            try
            {
              
                //Stripe events
                Event stripeEvent;
                try
                {
                    stripeEvent = EventUtility.ConstructEvent(
                        payload,
                        signature,
                        _stripeSettings.Value.WebhookSecret,
                        throwOnApiVersionMismatch: false
                    );
             
                }
                catch (StripeException stripeEx)
                {
                    throw;
                }
                switch (stripeEvent.Type)
                {
                    case "payment_intent.succeeded":
                        await HandlePaymentSucceededAsync(stripeEvent);
                        break;

                    case "payment_intent.payment_failed":
                        await HandlePaymentFailedAsync(stripeEvent);
                        break;

                    case "payment_intent.canceled":
                        await HandlePaymentCanceledAsync(stripeEvent);
                        break;

                    default:
                        break;
                }

                return Response<bool>.SuccessResponse(true);
            }
            catch (Exception ex)
            {
                return Response<bool>.FailResponse($"Error processing webhook: {ex.Message}");
            }
        }

        public async Task<Response<bool>> CancelStripePaymentAsync(string paymentIntentId)
        {
            try
            {
                var service = new PaymentIntentService();
                await service.CancelAsync(paymentIntentId);
                return Response<bool>.SuccessResponse(true);
            }
            catch (StripeException ex)
            {
                return Response<bool>.FailResponse($"Stripe error: {ex.Message}");
            }
        }
        #endregion
        #region Stripe Private Methods
        private async Task HandlePaymentSucceededAsync(Event stripeEvent)
        {
            try
            {

                // 1. Extract PaymentIntent from event
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                if (paymentIntent == null)
                {
                    return;
                }

                
                if (!paymentIntent.Metadata.TryGetValue("booking_id", out var bookingIdStr))
                {
                    return;
                }
                if (!int.TryParse(bookingIdStr, out var bookingId))
                {
                    return;
                }
                var payments = await _uow.Payments.GetPaymentsByBookingAsync(bookingId);
                var payment = payments.FirstOrDefault(p => p.TransactionId == paymentIntent.Id);

                if (payment == null)
                {
                    return;
                }


                //Update payment 
                payment.Update(
                    payment.Amount,
                    "stripe",
                    paymentIntent.Id,
                    PaymentStatus.Success,
                    DateTime.UtcNow
                );
                _uow.Payments.Update(payment);
                //Retrieve booking with related entities
                var booking = await _uow.Bookings.GetByIdAsync(bookingId);
                if (booking == null)
                {
                    await _uow.SaveChangesAsync(); // Still save payment update
                    return;
                }

              

                // Load related listing
                var listing = await _uow.Listings.GetByIdAsync(booking.ListingId);
                if (listing == null)
                {
                    await _uow.SaveChangesAsync(); // Still save payment update
                    return;
                }
                //Update booking
                booking.Update(
                    booking.CheckInDate,
                    booking.CheckOutDate,
                    booking.TotalPrice,
                    BookingPaymentStatus.Paid,
                    BookingStatus.Confirmed
                );
                _uow.Bookings.Update(booking);


                await _uow.SaveChangesAsync();

                try
                {
                    await _notificationService.CreateAsync(new CreateNotificationVM
                    {
                        UserId = booking.GuestId,
                        Title = "Payment Confirmed",
                        Body = $"Your payment for booking #{booking.Id} was successful! Get ready for your stay.",
                        Type = NotificationType.System,
                        CreatedAt = DateTime.UtcNow,
                        ActionUrl = $"/bookings/{booking.Id}",
                        ActionLabel = "View Booking"
                    });

                }
                catch (Exception notifEx)
                {
                    _logger?.LogError(
                        notifEx,
                        "Failed to send guest notification for BookingId={BookingId}",
                        booking.Id
                    );
                }
                try
                {
                    await _notificationService.CreateAsync(new CreateNotificationVM
                    {
                        UserId = listing.UserId,
                        Title = "New Booking Confirmed",
                        Body = $"You have a new confirmed booking for {listing.Title}. Check-in: {booking.CheckInDate:MMM dd, yyyy}",
                        Type = NotificationType.System,
                        CreatedAt = DateTime.UtcNow,
                        ActionUrl = $"/listings/{listing.Id}",
                        ActionLabel = "View Listing"
                    });
                }
                catch (Exception notifEx)
                {
                    _logger?.LogError(
                        notifEx,
                        "Failed to send host notification for ListingId={ListingId}",
                        listing.Id
                    );
                }

            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error handling payment succeeded event");
                throw; 
            }
        }

        private async Task HandlePaymentFailedAsync(Event stripeEvent)
        {
            try
            {

                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                if (paymentIntent == null)
                {
                    return;
                }

                if (!paymentIntent.Metadata.TryGetValue("booking_id", out var bookingIdStr) ||
                    !int.TryParse(bookingIdStr, out var bookingId))
                {
                    return;
                }

                var payments = await _uow.Payments.GetPaymentsByBookingAsync(bookingId);
                var payment = payments.FirstOrDefault(p => p.TransactionId == paymentIntent.Id);

                if (payment != null)
                {
                    payment.Update(
                        payment.Amount,
                        "stripe",
                        paymentIntent.Id,
                        PaymentStatus.Failed,
                        DateTime.UtcNow
                    );
                    _uow.Payments.Update(payment);

                    var booking = await _uow.Bookings.GetByIdAsync(bookingId);
                    if (booking != null)
                    {
                        // Notify guest about payment failure
                        try
                        {
                            await _notificationService.CreateAsync(new CreateNotificationVM
                            {
                                UserId = booking.GuestId,
                                Title = "Payment Failed",
                                Body = $"Your payment for booking #{booking.Id} failed. Please try again or contact support.",
                                Type = NotificationType.System,
                                CreatedAt = DateTime.UtcNow,
                                ActionUrl = $"/bookings/{booking.Id}",
                                ActionLabel = "Retry Payment"
                            });
                        }
                        catch (Exception notifEx)
                        {
                        }
                    }

                    await _uow.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error handling payment failed event");
                throw;
            }
        }

        private async Task HandlePaymentCanceledAsync(Event stripeEvent)
        {
            try
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                if (paymentIntent == null)
                {
                    _logger?.LogWarning("PaymentIntent is null in canceled event");
                    return;
                }
                if (!paymentIntent.Metadata.TryGetValue("booking_id", out var bookingIdStr) ||
                    !int.TryParse(bookingIdStr, out var bookingId))
                {
                    _logger?.LogWarning("Invalid or missing booking_id in canceled payment");
                    return;
                }

                var payments = await _uow.Payments.GetPaymentsByBookingAsync(bookingId);
                var payment = payments.FirstOrDefault(p => p.TransactionId == paymentIntent.Id);

                if (payment != null)
                {
                    payment.Update(
                        payment.Amount,
                        "stripe",
                        paymentIntent.Id,
                        PaymentStatus.Failed,
                        DateTime.UtcNow
                    );
                    _uow.Payments.Update(payment);
                    await _uow.SaveChangesAsync();

                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error handling payment canceled event");
                throw;
            }
        }
        #endregion

    }
}
