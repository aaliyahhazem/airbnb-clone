    namespace PL.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : BaseController
    {
        private readonly IIdentityService _identityService;
        private readonly IMessageService _messageService;
        
        

        public AuthController(IIdentityService identityService, IMessageService messageService)
        {
            _identityService = identityService;
            _messageService = messageService;
        }

        // Dev helper: return the decoded token payload / user id from claims
        [Authorize]
        [HttpGet("me/token-payload")]
        public IActionResult GetTokenPayload()
        {
            var payload = new Dictionary<string, string?>();
            var possible = new[] { ClaimTypes.NameIdentifier, "sub", JwtRegisteredClaimNames.Sub, "id", "uid" };
            foreach (var name in possible)
            {
                payload[name] = User.FindFirst(name)?.Value;
            }
            var nameClaim = User.Identity?.Name;
            payload["name"] = nameClaim;
            return Ok(payload);
        }

        // Dev helper: return the decoded token payload / user id from claims
        //[Authorize]
        //[HttpGet("me/token-payload")]
        //public IActionResult GetTokenPayload()
        //{
        //    var payload = new Dictionary<string, string?>();
        //    var possible = new[] { ClaimTypes.NameIdentifier, "sub", JwtRegisteredClaimNames.Sub, "id", "uid" };
        //    foreach (var name in possible)
        //    {
        //        payload[name] = User.FindFirst(name)?.Value;
        //    }
        //    var nameClaim = User.Identity?.Name;
        //    payload["name"] = nameClaim;
        //    return Ok(payload);
        //}
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
        
        /// <summary>
        /// Mark the user's onboarding as completed
        /// Called when user finishes the first-time walkthrough
        /// </summary>
        [Authorize]
        [HttpPut("complete-onboarding")]
        public async Task<IActionResult> CompleteOnboarding()
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();
            
            var res = await _identityService.CompleteOnboardingAsync(userId.Value);
            if (!res.Success) return BadRequest(res);
            
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
            var fullName = string.IsNullOrWhiteSpace(vm.FullName) ? "User" : vm.FullName;

            var token = _identityService.GenerateToken(userId, role, fullName, orderId, listingId);
            return Ok(new { token });
        }

        // Google auth using Firebase token
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto request)
        {
            if (string.IsNullOrWhiteSpace(request.IdToken))
                return BadRequest(new { errorMessage = "IdToken is required" });

            FirebaseToken decodedToken;

            try
            {
                decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(request.IdToken);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "Invalid Firebase token", details = ex.Message });
            }

            var email = decodedToken.Claims.TryGetValue("email", out var emailObj)
                ? emailObj?.ToString()
                : null;

            if (email == null)
                return BadRequest(new { errorMessage = "Email not provided by Google" });

            var fullName = decodedToken.Claims.TryGetValue("name", out var nObj)
                ? nObj?.ToString()
                : "User";

            var firebaseUid = decodedToken.Uid;

            var user = await _identityService.FindByEmailAsync(email);

            Response<LoginResponseVM> loginResponse;

            if (user == null)
            {
                var randomPassword = "Aa1!" + Guid.NewGuid().ToString("N")[..12];
                var username = email.Split('@')[0].ToLower();

                var registerResponse = await _identityService.RegisterAsync(
                    email: email,
                    password: randomPassword,
                    fullName: fullName,
                    userName: username,
                    firebaseUid: firebaseUid,
                    role: "Guest"
                );

                if (!registerResponse.Success)
                    return BadRequest(registerResponse);

                loginResponse = registerResponse;
            }
            else
            {
                var roles = await _identityService.GetRolesAsync(user.Id);
                var role = roles.FirstOrDefault() ?? "Guest";

                var token = _identityService.GenerateToken(user.Id, role, user.FullName);

                loginResponse = Response<LoginResponseVM>.SuccessResponse(new LoginResponseVM
                {
                    Token = token,
                    IsFirstLogin = user.IsFirstLogin,
                    User = new UserInfoVM
                    {
                        Id = user.Id,
                        Email = user.Email!,
                        UserName = user.UserName!,
                        FullName = user.FullName,
                        Role = role,
                        FirebaseUid = firebaseUid
                    }
                });
            }

            return Ok(loginResponse);
        }

    }
}
