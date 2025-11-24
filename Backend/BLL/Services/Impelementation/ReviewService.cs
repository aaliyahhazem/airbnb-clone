namespace BLL.Services.Impelementation
{
    public class ReviewService : IReviewService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IReviewRepository _reviewRepo;

        public ReviewService(IUnitOfWork uow, IMapper mapper, IReviewRepository reviewRepo)
        {
            _uow = uow;
            _mapper = mapper;
            _reviewRepo = reviewRepo;
        }

        public async Task<Response<ReviewVM>> CreateReviewAsync(CreateReviewVM model)
        {
            try
            {
                var entity = DAL.Entities.Review.Create(model.BookingId, model.GuestId, model.Rating, model.Comment, model.CreatedAt);
                await _uow.Reviews.AddAsync(entity);
                await _uow.SaveChangesAsync();
                var vm = _mapper.Map<ReviewVM>(entity);
                return Response<ReviewVM>.SuccessResponse(vm);
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
                _uow.Reviews.Update(existing);
                await _uow.SaveChangesAsync();
                var vm = _mapper.Map<ReviewVM>(existing);
                return Response<ReviewVM>.SuccessResponse(vm);
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
                _uow.Reviews.Delete(existing);
                await _uow.SaveChangesAsync();
                return Response<bool>.SuccessResponse(true);
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
                var list = await _reviewRepo.GetReviewsByGuestAsync(guestId);
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
