using BLL.ModelVM.Admin;
using Microsoft.AspNetCore.Mvc;

namespace PL.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // Temporarily disable authorization for local testing. Re-enable in production.
    // [Authorize(Roles = "Admin")]
    public class AdminController : BaseController
    {
        private readonly IAdminService _adminService;
        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] string? search, [FromQuery] string? role, [FromQuery] bool? isActive, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var res = await _adminService.GetUsersFilteredAsync(search, role, isActive, page, pageSize);
            return Ok(res);
        }

        [HttpGet("users/{id:guid}")]
        public async Task<IActionResult> GetUser(Guid id)
        {
            var res = await _adminService.GetUserByIdAsync(id);
            return Ok(res);
        }

        [HttpPut("users/{id:guid}/deactivate")]
        public async Task<IActionResult> DeactivateUser(Guid id)
        {
            var res = await _adminService.DeactivateUserAsync(id);
            return Ok(res);
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var res = await _adminService.GetSystemStatsAsync();
            return Ok(res);
        }

        [HttpGet("listings")]
        public async Task<IActionResult> GetListings([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var res = await _adminService.GetListingsFilteredAsync(status, page, pageSize);
            return Ok(res);
        }

        [HttpGet("listings/pending")]
        public async Task<IActionResult> GetPendingListings()
        {
            var res = await _adminService.GetListingsPendingApprovalAsync();
            return Ok(res);
        }

        [HttpGet("bookings")]
        public async Task<IActionResult> GetBookings([FromQuery] int page = 1, [FromQuery] int pageSize = 100)
        {
            var res = await _adminService.GetBookingsDetailedAsync(page, pageSize);
            return Ok(res);
        }

        [HttpGet("revenue/trend")]
        public async Task<IActionResult> GetRevenueTrend([FromQuery] int months = 12)
        {
            var res = await _adminService.GetRevenueTrendAsync(months);
            return Ok(res);
        }

        [HttpGet("promotions/active")]
        public async Task<IActionResult> GetActivePromotions()
        {
            var res = await _adminService.GetActivePromotionsAsync();
            return Ok(res);
        }

        [HttpGet("promotions/history")]
        public async Task<IActionResult> GetPromotionsHistory([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var res = await _adminService.GetPromotionsHistoryAsync(from, to);
            return Ok(res);
        }

        [HttpGet("promotions/expiring")]
        public async Task<IActionResult> GetExpiringPromotions([FromQuery] int days = 7)
        {
            var res = await _adminService.GetExpiringPromotionsAsync(days);
            return Ok(res);
        }
    }
}
