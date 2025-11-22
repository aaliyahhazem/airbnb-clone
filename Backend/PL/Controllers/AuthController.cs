using System.Globalization;

namespace PL.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : BaseController
    {
        private readonly IIdentityService _identityService;

        public AuthController(IIdentityService identityService)
        {
            _identityService = identityService;
        }
        //any user regester as a guest
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterVM vm)
        {
            var res = await _identityService.RegisterAsync(vm.Email, vm.Password, vm.FullName, vm.UserName, vm.FirebaseUid, "Guest");
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginVM vm)
        {
            var res = await _identityService.LoginAsync(vm.Email, vm.Password);
            if (!res.Success) return Unauthorized(res);
            return Ok(res);
        }



        [HttpPost("send-password-reset")]
        public async Task<IActionResult> SendPasswordReset([FromBody] EmailVM vm)
        {
            var res = await _identityService.SendPasswordResetAsync(vm.Email);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordVM vm)
        {
            var res = await _identityService.ResetPasswordAsync(vm.Email, vm.Token, vm.NewPassword);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        [HttpPost("oauth")]
        public async Task<IActionResult> OAuth([FromBody] OAuthVM vm)
        {
            var res = await _identityService.OAuthLoginAsync(vm.Provider, vm.ExternalToken);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        [Authorize]
        [HttpPost("verify-face")]
        public async Task<IActionResult> VerifyFace([FromBody] FaceVM vm)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();
            var res = await _identityService.VerifyFaceIdAsync(userId.Value, vm.FaceData);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        //toggle user role guest / host
        [Authorize]
        [HttpPost("toggle-role")]
        public async Task<IActionResult> ToggleRole()
        {
            var userId = GetUserIdFromClaims();
            
            if (userId == null) return Unauthorized(); 
            var res = await _identityService.ToggleUserRoleAsync(userId.Value);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        //make user admin
        [Authorize(Roles = "Admin")]
        [HttpPost("make-admin/{userId}")]
        public async Task<IActionResult> MakeAdmin([FromRoute] Guid userId)
        {
            var res = await _identityService.MakeUserAdminAsync(userId);
            if (!res.Success) return BadRequest(res); 
            return Ok(res);
        }

        [HttpPost("token")]
        public IActionResult Token([FromBody] TokenRequestVM vm)
        {
            if (vm == null) return BadRequest("Request body is required.");

            if (!Guid.TryParse(vm.UserId, out var userId))
                return BadRequest("Invalid UserId GUID.");

            Guid? orderId = null;
            if (!string.IsNullOrWhiteSpace(vm.OrderId))
            {
                if (Guid.TryParse(vm.OrderId, out var o)) orderId = o;
                else return BadRequest("Invalid OrderId GUID.");
            }

            Guid? listingId = null;
            if (!string.IsNullOrWhiteSpace(vm.ListingId))
            {
                if (Guid.TryParse(vm.ListingId, out var l)) listingId = l;
                else return BadRequest("Invalid ListingId GUID.");
            }

            var role = string.IsNullOrWhiteSpace(vm.Role) ? "Guest" : vm.Role;

            var token = _identityService.GenerateToken(userId, role, orderId, listingId);
            return Ok(new { token });
        }
    }
}
