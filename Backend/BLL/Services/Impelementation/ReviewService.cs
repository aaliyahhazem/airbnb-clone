using Microsoft.EntityFrameworkCore;

namespace BLL.Services.Impelementation
{
    public class ReviewService : IReviewService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public ReviewService(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<Response<ReviewVM>> CreateReviewAsync(CreateReviewVM model, Guid userId)
        {
            try
            {
                // Validate input
                if (model == null) return Response<ReviewVM>.FailResponse("Request model is required.");
                if (model.Rating < 1 || model.Rating > 5) return Response<ReviewVM>.FailResponse("Rating must be between 1 and 5.");

                // Ensure booking exists
                var booking = await _uow.Bookings.GetByIdAsync(model.BookingId);
                if (booking == null) return Response<ReviewVM>.FailResponse("Booking not found.");

                // Ensure the user creating the review is the guest who made the booking
                if (booking.GuestId != userId) return Response<ReviewVM>.FailResponse("User is not the guest for this booking.");

                // Prevent duplicate review for same booking
                var existingReviews = await _uow.Reviews.GetReviewsByBookingAsync(model.BookingId);
                if (existingReviews != null && existingReviews.Any())
                    return Response<ReviewVM>.FailResponse("A review for this booking already exists.");

                // Use domain factory to ensure invariants and required fields are set
                var entity = await _uow.Reviews.CreateAsync(model.BookingId, userId, model.Rating, model.Comment, DateTime.UtcNow);
                var vm = _mapper.Map<ReviewVM>(entity);
                return Response<ReviewVM>.SuccessResponse(vm);
            }
            catch (DbUpdateException dbEx)
            {
                // Return inner exception message or full exception details to help identify DB-level errors
                var msg = dbEx.InnerException?.Message ?? dbEx.ToString();
                return Response<ReviewVM>.FailResponse(msg);
            }
            catch (Exception ex)
            {
                return Response<ReviewVM>.FailResponse(ex.Message);
            }
        }

        public async Task<Response<ReviewVM>> UpdateReviewAsync(int id, UpdateReviewVM model)
        {
            try
            {
                var existing = await _uow.Reviews.GetByIdAsync(id);
                if (existing == null) return Response<ReviewVM>.FailResponse("Review not found");
                existing.Update(model.Rating, model.Comment); 
                await _uow.Reviews.UpdateAsync(existing);
                var vm = _mapper.Map<ReviewVM>(existing);
                return Response<ReviewVM>.SuccessResponse(vm);
            }
            catch (DbUpdateException dbEx)
            {
                // Return inner exception message or full exception details to help identify DB-level errors
                var msg = dbEx.InnerException?.Message ?? dbEx.ToString();
                return Response<ReviewVM>.FailResponse(msg);
            }
            catch (Exception ex)
            {
                return Response<ReviewVM>.FailResponse(ex.Message);
            }
        }

        public async Task<Response<bool>> DeleteReviewAsync(int id)
        {
            try
            {
                var existing = await _uow.Reviews.GetByIdAsync(id);
                if (existing == null) return Response<bool>.FailResponse("Review not found");
                var ok = await _uow.Reviews.DeleteAsync(existing);
                return ok ? Response<bool>.SuccessResponse(true) : Response<bool>.FailResponse("Failed to delete");
            }
            catch (DbUpdateException dbEx)
            {
                // Return inner exception message or full exception details to help identify DB-level errors
                var msg = dbEx.InnerException?.Message ?? dbEx.ToString();
                return Response<bool>.FailResponse(msg);
            }
            catch (Exception ex)
            {
                return Response<bool>.FailResponse(ex.Message);
            }
        }

        public async Task<Response<List<ReviewVM>>> GetReviewsByListingAsync(int listingId)
        {
            try
            {
                var list = await _uow.Reviews.GetReviewsByBookingAsync(listingId); // bookingId used - might need listing mapping
                var mapped = _mapper.Map<List<ReviewVM>>(list);
                return Response<List<ReviewVM>>.SuccessResponse(mapped);
            }
            catch (Exception ex)
            {
                return Response<List<ReviewVM>>.FailResponse(ex.Message);
            }
        }

        public async Task<Response<List<ReviewVM>>> GetReviewsByGuestAsync(Guid guestId)
        {
            try
            {
                var list = await _uow.Reviews.GetReviewsByGuestAsync(guestId); 
                var mapped = _mapper.Map<List<ReviewVM>>(list);
                return Response<List<ReviewVM>>.SuccessResponse(mapped);
            }
            catch (Exception ex)
            {
                return Response<List<ReviewVM>>.FailResponse(ex.Message);
            }
        }

        public async Task<Response<double>> GetAverageRatingAsync(int listingId)
        {
            try
            {
                // compute avg rating for a listing by joining bookings -> reviews
                var reviews = await _uow.Reviews.GetAllAsync();
                var filtered = reviews.Where(r => r.Booking != null && r.Booking.ListingId == listingId);
                var avg = filtered.Any() ? filtered.Average(r => r.Rating) : 0.0;
                return Response<double>.SuccessResponse(avg);
            }
            catch (Exception ex)
            {
                return Response<double>.FailResponse(ex.Message);
            }
        }
    }
}
