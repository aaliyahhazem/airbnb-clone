

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
                // Log key inputs for diagnostics (dev-only, non-sensitive)
                try
                {
                    _logger?.LogInformation("CreateBookingAsync called by guest={GuestId} listing={ListingId} checkIn={CheckIn} checkOut={CheckOut} guests={Guests} method={PaymentMethod}",
                        guestId, model.ListingId, model.CheckInDate.ToString("o"), model.CheckOutDate.ToString("o"), model.Guests, model.PaymentMethod);
                }
                catch { }
                // basic validations
                var listing = await _uow.Listings.GetByIdAsync(model.ListingId);
                if (listing == null) return Response<GetBookingVM>.FailResponse("Listing not found");
                if (model.CheckOutDate <= model.CheckInDate) return Response<GetBookingVM>.FailResponse("Invalid dates");

                // ensure guest exists
                var guest = await _uow.Users.GetByIdAsync(guestId);
                if (guest == null) return Response<GetBookingVM>.FailResponse("Guest not found");

                // check availability: any overlapping active booking?
                var existing = (await _uow.Bookings.GetBookingsByListingAsync(model.ListingId))
                .Where(b => b.BookingStatus == BookingStatus.Pending && !(model.CheckOutDate <= b.CheckInDate || model.CheckInDate >= b.CheckOutDate));
                if (existing.Any()) return Response<GetBookingVM>.FailResponse("Listing is not available for the selected dates");

                // calculate total price (simple nights * price)
                var nights = (decimal)(model.CheckOutDate - model.CheckInDate).TotalDays;
                if (nights <= 0) return Response<GetBookingVM>.FailResponse("Invalid date range");
                var total = nights * listing.PricePerNight;

                Booking created = null!;
                CreatePaymentIntentVm? intentResult = null;
                await _uow.ExecuteInTransactionAsync(async () =>
                {
                    var bookingEntity = await _uow.Bookings.CreateAsync(model.ListingId, guestId, model.CheckInDate, model.CheckOutDate, total);
                    await _uow.SaveChangesAsync();

                    // ✅ CREATE STRIPE INTENT FOR STRIPE PAYMENTS
                    if (model.PaymentMethod.ToLower() == "stripe")
                    {
                        var stripePayload = new CreateStripePaymentVM
                        {
                            BookingId = bookingEntity.Id,
                            Amount = total,
                            Currency = "usd",
                            Description = $"Booking #{bookingEntity.Id}"
                        };

                        var paymentResp = await _paymentService.CreateStripePaymentIntentAsync(guestId, stripePayload);
                        if (!paymentResp.Success)
                            throw new Exception(paymentResp.errorMessage ?? "Stripe payment intent creation failed");

                        intentResult = paymentResp.result;
                    }
                    else
                    {
                        // Non-Stripe payment methods (if you have any)
                        var paymentResp = await _paymentService.InitiatePaymentAsync(guestId, bookingEntity.Id, total, model.PaymentMethod);
                        if (!paymentResp.Success)
                            throw new Exception(paymentResp.errorMessage ?? "Payment initiation failed");
                    }

                    // Increment booking priority for engagement tracking
                    await _uow.Listings.IncrementBookingPriorityAsync(model.ListingId);
                    await _uow.SaveChangesAsync();

                    // load created booking for return
                    created = bookingEntity;
                });
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
                return Response<GetBookingVM>.SuccessResponse(vm);
            }
            catch (Exception ex)
            {
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
                if (booking == null) return Response<GetBookingVM>.FailResponse("Booking not found");

                // allow guests and hosts to fetch
                if (booking.GuestId != requesterId)
                {
                    var listing = await _uow.Listings.GetByIdAsync(booking.ListingId);
                    if (listing == null) return Response<GetBookingVM>.FailResponse("Booking/listing not found");
                    if (listing.UserId != requesterId) return Response<GetBookingVM>.FailResponse("Not authorized");
                }

                var vm = new GetBookingVM
                {
                    Id = booking.Id,
                    ListingId = booking.ListingId,
                    CheckInDate = booking.CheckInDate,
                    CheckOutDate = booking.CheckOutDate,
                    TotalPrice = booking.TotalPrice,
                    BookingStatus = booking.BookingStatus.ToString(),
                    PaymentStatus = booking.PaymentStatus.ToString()
                };

                return Response<GetBookingVM>.SuccessResponse(vm);
            }
            catch (Exception ex)
            {
                return Response<GetBookingVM>.FailResponse(ex.Message);
            }
        }
    }
}
