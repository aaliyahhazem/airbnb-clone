

using Microsoft.Extensions.Logging;
using Stripe;

namespace BLL.Services.Impelementation
{
    public class BookingService : IBookingService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IPaymentService _paymentService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<BookingService> _logger;

        public BookingService(IUnitOfWork uow, IMapper mapper, IPaymentService paymentService, INotificationService notificationService, ILogger<BookingService> logger)
        {
            _uow = uow;
            _mapper = mapper;
            _paymentService = paymentService;
            _notificationService = notificationService;
            _logger = logger;
        }

        // Create booking: check availability, calculate price, create booking and initiate payment atomically
        public async Task<Response<GetBookingVM>> CreateBookingAsync(Guid guestId, CreateBookingVM model)
        {
            try
            {
                _logger?.LogInformation(
                    "CreateBookingAsync called by guest={GuestId} listing={ListingId} " +
                    "checkIn={CheckIn} checkOut={CheckOut} guests={Guests} method={Method}",
                    guestId, model.ListingId, model.CheckInDate, model.CheckOutDate,
                    model.Guests, model.PaymentMethod);

                // 1. Validate listing exists
                var listing = await _uow.Listings.GetByIdAsync(model.ListingId);
                if (listing == null)
                    return Response<GetBookingVM>.FailResponse("Listing not found");

                // 2. Validate dates
                if (model.CheckInDate >= model.CheckOutDate)
                    return Response<GetBookingVM>.FailResponse("Check-out must be after check-in");

                if (model.CheckInDate < DateTime.UtcNow.Date)
                    return Response<GetBookingVM>.FailResponse("Check-in date cannot be in the past");

                // 3. Validate guest capacity
                if (model.Guests > listing.MaxGuests)
                    return Response<GetBookingVM>.FailResponse(
                        $"Listing can accommodate maximum {listing.MaxGuests} guests");

                //  Check availability ONLY for CONFIRMED/PAID bookings
                var conflictingBookings = await _uow.Bookings.GetBookingsByListingAsync(model.ListingId);

                var hasConflict = conflictingBookings.Any(b =>
                    // ⚠️ CRITICAL: Only block if booking is confirmed/paid AND dates overlap
                    (b.BookingStatus == BookingStatus.Confirmed ||
                     b.PaymentStatus == BookingPaymentStatus.Paid) &&
                    b.CheckInDate < model.CheckOutDate &&
                    b.CheckOutDate > model.CheckInDate
                );

                if (hasConflict)
                {
                    _logger?.LogWarning(
                        "Listing {ListingId} has conflicting bookings for {CheckIn} to {CheckOut}",
                        model.ListingId, model.CheckInDate, model.CheckOutDate);

                    return Response<GetBookingVM>.FailResponse(
                        "Listing is not available for the selected dates");
                }

                // Calculate total price
                var nights = (model.CheckOutDate - model.CheckInDate).Days;
                var total = listing.PricePerNight * nights;

                Booking created = null!;
                CreatePaymentIntentVm? intentResult = null;

                await _uow.ExecuteInTransactionAsync(async () =>
                {
                    // Create booking
                    var bookingEntity = await _uow.Bookings.CreateAsync(
                        model.ListingId, guestId, model.CheckInDate,
                        model.CheckOutDate, total);

                    await _uow.SaveChangesAsync();

                    _logger?.LogInformation("Booking {BookingId} created for listing {ListingId}",
                        bookingEntity.Id, model.ListingId);

                    // Create Stripe Payment Intent
                    if (model.PaymentMethod.ToLower() == "stripe")
                    {
                        var stripePayload = new CreateStripePaymentVM
                        {
                            BookingId = bookingEntity.Id,
                            Amount = total,
                            Currency = "usd",
                            Description = $"Booking #{bookingEntity.Id} - {listing.Title}"
                        };

                        var paymentResp = await _paymentService
                            .CreateStripePaymentIntentAsync(guestId, stripePayload);

                        if (!paymentResp.Success)
                        {
                            _logger?.LogError(
                                "Payment intent creation failed for booking {BookingId}: {Error}",
                                bookingEntity.Id, paymentResp.errorMessage);

                            throw new Exception(paymentResp.errorMessage
                                ?? "Failed to create payment intent");
                        }

                        intentResult = paymentResp.result;

                        _logger?.LogInformation(
                            "Payment intent {PaymentIntentId} created for booking {BookingId}",
                            intentResult.PaymentIntentId, bookingEntity.Id);
                    }

                    await _uow.Listings.IncrementBookingPriorityAsync(model.ListingId);
                    await _uow.SaveChangesAsync();

                    created = bookingEntity;
                });

                // ✅ Build complete response
                var vm = new GetBookingVM
                {
                    Id = created.Id,
                    ListingId = created.ListingId,
                    CheckInDate = created.CheckInDate,
                    CheckOutDate = created.CheckOutDate,
                    TotalPrice = created.TotalPrice,
                    BookingStatus = created.BookingStatus.ToString(),
                    PaymentStatus = created.PaymentStatus.ToString(),
                    ClientSecret = intentResult?.ClientSecret,
                    PaymentIntentId = intentResult?.PaymentIntentId
                };

                _logger?.LogInformation(
                    "Booking {BookingId} created successfully with PaymentIntent={PaymentIntentId}, " +
                    "ClientSecret={HasSecret}",
                    vm.Id, vm.PaymentIntentId, !string.IsNullOrEmpty(vm.ClientSecret));

                return Response<GetBookingVM>.SuccessResponse(vm);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex,
                    "CreateBookingAsync failed for guest {GuestId}, listing {ListingId}",
                    guestId, model.ListingId);

                return Response<GetBookingVM>.FailResponse(ex.Message);
            }
        }
        public async Task<Response<bool>> CancelBookingAsync(Guid guestId, int bookingId)
        {
            try
            {
                var booking = await _uow.Bookings.GetByIdAsync(bookingId);
                if (booking == null) return Response<bool>.FailResponse("Booking not found");
                if (booking.GuestId != guestId) return Response<bool>.FailResponse("Not authorized");

                // simple policy: allow cancel if more than24h before checkin
                if ((booking.CheckInDate - DateTime.UtcNow).TotalHours < 24) return Response<bool>.FailResponse("Cancellation period expired");

                booking.Update(booking.CheckInDate, booking.CheckOutDate, booking.TotalPrice, booking.PaymentStatus, BookingStatus.Cancelled);
                _uow.Bookings.Update(booking);
                await _uow.SaveChangesAsync();

                // notify guest about cancellation
                await _notificationService.CreateAsync(new BLL.ModelVM.Notification.CreateNotificationVM
                {
                    UserId = guestId,
                    Title = "Booking Cancelled",
                    Body = "Your booking has been cancelled successfully.",
                    CreatedAt = DateTime.UtcNow,
                    ActionUrl = "/booking",
                    ActionLabel = "View Bookings"
                });

                // notify host about cancellation
                var listing = await _uow.Listings.GetByIdAsync(booking.ListingId);
                if (listing != null)
                {
                    await _notificationService.CreateAsync(new BLL.ModelVM.Notification.CreateNotificationVM
                    {
                        UserId = listing.UserId,
                        Title = "Booking Cancelled",
                        Body = $"A guest cancelled their booking for {listing.Title}.",
                        CreatedAt = DateTime.UtcNow,
                        ActionUrl = $"/listings/{listing.Id}",
                        ActionLabel = "View Listing"
                    });
                }

                // if paid -> refund
                if (booking.Payment != null && booking.Payment.Status == PaymentStatus.Success)
                {
                    await _paymentService.RefundPaymentAsync(booking.Payment.Id);
                }

                return Response<bool>.SuccessResponse(true);
            }
            catch (Exception ex)
            {
                return Response<bool>.FailResponse(ex.Message);
            }
        }

        public async Task<Response<List<GetBookingVM>>> GetBookingsByUserAsync(Guid userId)
        {
            try
            {
                var bookings = await _uow.Bookings.GetBookingsByGuestAsync(userId);
                var mapped = bookings.Select(b => new GetBookingVM { Id = b.Id, ListingId = b.ListingId, CheckInDate = b.CheckInDate, CheckOutDate = b.CheckOutDate, TotalPrice = b.TotalPrice, BookingStatus = b.BookingStatus.ToString(), PaymentStatus = b.PaymentStatus.ToString() }).ToList();
                return Response<List<GetBookingVM>>.SuccessResponse(mapped);
            }
            catch (Exception ex)
            {
                return Response<List<GetBookingVM>>.FailResponse(ex.Message);
            }
        }

        public async Task<Response<List<GetBookingVM>>> GetBookingsByHostAsync(Guid hostId)
        {
            try
            {
                var listings = await _uow.Listings.GetAllAsync();
                var hostListingIds = listings.Where(l => l.UserId == hostId).Select(l => l.Id).ToList();
                var allBookings = await _uow.Bookings.GetAllAsync();
                var hostBookings = allBookings.Where(b => hostListingIds.Contains(b.ListingId)).Select(b => new GetBookingVM { Id = b.Id, ListingId = b.ListingId, CheckInDate = b.CheckInDate, CheckOutDate = b.CheckOutDate, TotalPrice = b.TotalPrice, BookingStatus = b.BookingStatus.ToString(), PaymentStatus = b.PaymentStatus.ToString() }).ToList();
                return Response<List<GetBookingVM>>.SuccessResponse(hostBookings);
            }
            catch (Exception ex)
            {
                return Response<List<GetBookingVM>>.FailResponse(ex.Message);
            }
        }

            public async Task<Response<GetBookingVM>> GetByIdAsync(Guid requesterId, int bookingId)
        {
            try
            {
                var booking = await _uow.Bookings.GetByIdAsync(bookingId);
                if (booking == null)
                    return Response<GetBookingVM>.FailResponse("Booking not found");

                // Allow guests and hosts to fetch
                if (booking.GuestId != requesterId)
                {
                    var listing = await _uow.Listings.GetByIdAsync(booking.ListingId);
                    if (listing == null)
                        return Response<GetBookingVM>.FailResponse("Booking/listing not found");
                    if (listing.UserId != requesterId)
                        return Response<GetBookingVM>.FailResponse("Not authorized");
                }

                // ✅ Get payment details to include clientSecret if available
                var payments = await _uow.Payments.GetPaymentsByBookingAsync(bookingId);
                var latestPayment = payments
                    .OrderByDescending(p => p.PaidAt)
                    .FirstOrDefault();

                // ✅ If payment exists and has a Stripe PaymentIntentId, try to get clientSecret
                string? clientSecret = null;
                string? paymentIntentId = booking.PaymentIntentId;

                if (!string.IsNullOrEmpty(paymentIntentId) && latestPayment?.Status == PaymentStatus.Pending)
                {
                    try
                    {
                        // Retrieve PaymentIntent from Stripe to get clientSecret
                        var service = new Stripe.PaymentIntentService();
                        var intent = await service.GetAsync(paymentIntentId);
                        clientSecret = intent?.ClientSecret;

                        _logger?.LogInformation(
                            "Retrieved clientSecret for booking {BookingId}, PaymentIntent {PaymentIntentId}",
                            bookingId, paymentIntentId);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex,
                            "Could not retrieve PaymentIntent {PaymentIntentId} for booking {BookingId}",
                            paymentIntentId, bookingId);
                    }
                }

                var vm = new GetBookingVM
                {
                    Id = booking.Id,
                    ListingId = booking.ListingId,
                    CheckInDate = booking.CheckInDate,
                    CheckOutDate = booking.CheckOutDate,
                    TotalPrice = booking.TotalPrice,
                    BookingStatus = booking.BookingStatus.ToString(),
                    PaymentStatus = booking.PaymentStatus.ToString(),
                    ClientSecret = clientSecret,
                    PaymentIntentId = paymentIntentId
                };

                return Response<GetBookingVM>.SuccessResponse(vm);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in GetByIdAsync for booking {BookingId}", bookingId);
                return Response<GetBookingVM>.FailResponse(ex.Message);
            }
        }
    }
}
