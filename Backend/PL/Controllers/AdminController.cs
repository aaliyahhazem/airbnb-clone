namespace PL.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : BaseController
    {
        private readonly IAdminService _adminService;
        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var res = await _adminService.GetAllUsersAsync();
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
        public async Task<IActionResult> GetListings()
        {
            var res = await _adminService.GetAllListingsAsync();
            return Ok(res);
        }

        [HttpGet("bookings")]
        public async Task<IActionResult> GetBookings()
        {
            var res = await _adminService.GetAllBookingsAsync();
            return Ok(res);
        }
    }
}
