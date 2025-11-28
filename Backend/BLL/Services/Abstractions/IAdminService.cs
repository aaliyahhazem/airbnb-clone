using BLL.ModelVM.Admin;

namespace BLL.Services.Abstractions
{
    public interface IAdminService
    {
        Task<Response<List<UserSummaryVM>>> GetAllUsersAsync();
        Task<Response<UserSummaryVM>> GetUserByIdAsync(Guid id);
        Task<Response<bool>> DeactivateUserAsync(Guid id);
        Task<Response<SystemStatsVM>> GetSystemStatsAsync();
        Task<Response<List<Listing>>> GetAllListingsAsync();
        Task<Response<List<Booking>>> GetAllBookingsAsync();

        // New admin endpoints for dashboard
        Task<Response<List<UserAdminVM>>> GetUsersFilteredAsync(string? search = null, string? role = null, bool? isActive = null, int page = 1, int pageSize = 50);
        Task<Response<List<ListingAdminVM>>> GetListingsFilteredAsync(string? status = null, int page = 1, int pageSize = 50);
        Task<Response<List<ListingAdminVM>>> GetListingsPendingApprovalAsync();
        Task<Response<List<BookingAdminVM>>> GetBookingsDetailedAsync(int page = 1, int pageSize = 100);
        Task<Response<List<RevenuePointVM>>> GetRevenueTrendAsync(int months = 12);
        Task<Response<List<PromotionSummaryVM>>> GetActivePromotionsAsync();
        Task<Response<List<PromotionSummaryVM>>> GetPromotionsHistoryAsync(DateTime? from = null, DateTime? to = null);
        Task<Response<List<PromotionSummaryVM>>> GetExpiringPromotionsAsync(int days = 7);
    }
}
