

namespace BLL.Services.Abstractions
{
 public interface IReviewService
 {
 Task<Response<ReviewVM>> CreateReviewAsync(CreateReviewVM model, Guid userId);
 Task<Response<ReviewVM>> UpdateReviewAsync(int id, UpdateReviewVM model);
 Task<Response<bool>> DeleteReviewAsync(int id);
 Task<Response<List<ReviewVM>>> GetReviewsByListingAsync(int listingId);
 Task<Response<List<ReviewVM>>> GetReviewsByGuestAsync(Guid guestId);
 Task<Response<double>> GetAverageRatingAsync(int listingId);
 }
}
