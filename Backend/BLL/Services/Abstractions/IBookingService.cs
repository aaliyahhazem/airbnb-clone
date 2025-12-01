
namespace BLL.Services.Abstractions
{
    public interface IBookingService
    {
        Task<Response<GetBookingVM>> CreateBookingAsync(Guid guestId, CreateBookingVM model);
        Task<Response<bool>> CancelBookingAsync(Guid guestId, int bookingId);
        Task<Response<List<GetBookingVM>>> GetBookingsByUserAsync(Guid userId);
        Task<Response<List<GetBookingVM>>> GetBookingsByHostAsync(Guid hostId);
        Task<Response<GetBookingVM>> GetByIdAsync(Guid requesterId, int bookingId);
    }
}
