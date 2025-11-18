
using DAL.Enum;
using Microsoft.AspNetCore.Mvc;

namespace PL.Controllers
{
 [Route("api/[controller]")]
 [ApiController]
 public class AuthController : ControllerBase
 {
 private readonly IIdentityService _identityService;

 public AuthController(IIdentityService identityService)
 {
 _identityService = identityService;
 }
        [Authorize(Roles = nameof(UserRole.Admin))]
        [HttpPost("register")]
 public async Task<IActionResult> Register([FromBody] RegisterDto dto)
 {
 var res = await _identityService.RegisterAsync(dto.Email, dto.Password, dto.FullName, dto.FirebaseUid, dto.Role);
 if (!res.Success) return BadRequest(res);
 return Ok(res);
 }

 [HttpPost("login")]
 public async Task<IActionResult> Login([FromBody] LoginDto dto)
 {
 var res = await _identityService.LoginAsync(dto.Email, dto.Password);
 if (!res.Success) return Unauthorized(res);
 return Ok(res);
 }

 [HttpPost("send-password-reset")]
 public async Task<IActionResult> SendPasswordReset([FromBody] EmailDto dto)
 {
 var res = await _identityService.SendPasswordResetAsync(dto.Email);
 if (!res.Success) return BadRequest(res);
 return Ok(res);
 }

 [HttpPost("reset-password")]
 public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
 {
 var res = await _identityService.ResetPasswordAsync(dto.Email, dto.Token, dto.NewPassword);
 if (!res.Success) return BadRequest(res);
 return Ok(res);
 }

 [HttpPost("oauth")]
 public async Task<IActionResult> OAuth([FromBody] OAuthDto dto)
 {
 var res = await _identityService.OAuthLoginAsync(dto.Provider, dto.ExternalToken);
 if (!res.Success) return BadRequest(res);
 return Ok(res);
 }

 [Authorize]
 [HttpPost("verify-face")]
 public async Task<IActionResult> VerifyFace([FromBody] FaceDto dto)
 {
 var sub = User.FindFirst("sub")?.Value;
 if (string.IsNullOrWhiteSpace(sub)) return Unauthorized();
 var userId = Guid.Parse(sub);
 var res = await _identityService.VerifyFaceIdAsync(userId, dto.FaceData);
 if (!res.Success) return BadRequest(res);
 return Ok(res);
 }

 [HttpPost("token")]
 public IActionResult Token([FromBody] TokenRequestDto dto)
 {
 if (dto == null) return BadRequest("Request body is required.");

 if (!Guid.TryParse(dto.UserId, out var userId))
 return BadRequest("Invalid UserId GUID.");

 Guid? orderId = null;
 if (!string.IsNullOrWhiteSpace(dto.OrderId))
 {
 if (Guid.TryParse(dto.OrderId, out var o)) orderId = o;
 else return BadRequest("Invalid OrderId GUID.");
 }

 Guid? listingId = null;
 if (!string.IsNullOrWhiteSpace(dto.ListingId))
 {
 if (Guid.TryParse(dto.ListingId, out var l)) listingId = l;
 else return BadRequest("Invalid ListingId GUID.");
 }

 var role = string.IsNullOrWhiteSpace(dto.Role) ? "Guest" : dto.Role;

 var token = _identityService.GenerateToken(userId, role, orderId, listingId);
 return Ok(new { token });
 }
 }

 // DTOs
 public record RegisterDto(string Email, string Password, string FullName, string? FirebaseUid, string Role = "Guest");
 public record LoginDto(string Email, string Password);
 public record EmailDto(string Email);
 public record ResetPasswordDto(string Email, string Token, string NewPassword);
 public record OAuthDto(string Provider, string ExternalToken);
 public record FaceDto(string FaceData);
 public record TokenRequestDto(string UserId, string Role, string? OrderId, string? ListingId);
}
