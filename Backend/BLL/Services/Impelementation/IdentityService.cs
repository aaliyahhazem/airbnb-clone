using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace BLL.Services.Impelementation
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly ITokenService _tokenService;
        private readonly INotificationService _notificationService;
        private readonly IMessageService _messageService;
        private readonly IUnitOfWork _unitOfWork;

        public IdentityService(
            UserManager<User> userManager, 
            SignInManager<User> signInManager, 
            RoleManager<IdentityRole<Guid>> roleManager, 
            ITokenService tokenService,
            INotificationService notificationService,
            IMessageService messageService,
            IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _tokenService = tokenService;
            _notificationService = notificationService;
            _messageService = messageService;
            _unitOfWork = unitOfWork;
        }

        public async Task<Response<LoginResponseVM>> RegisterAsync(string email, string password, string fullName, string userName, string? firebaseUid = null, string role = "Guest")
        {
            try
            {
                Console.WriteLine($"RegisterAsync called: email={email}, firebaseUid={firebaseUid}");

                var existing = await _userManager.FindByEmailAsync(email);
                if (existing != null)
                {
                    return Response<LoginResponseVM>.FailResponse("Email already registered");
                }

                // Check for username uniqueness
                var existingUserName = await _userManager.FindByNameAsync(userName);
                if (existingUserName != null)
                {
                    return Response<LoginResponseVM>.FailResponse("Username already taken");
                }

                // Sanitize username
                string sanitized = null;
                if (!string.IsNullOrWhiteSpace(userName))
                {
                    sanitized = Regex.Replace(userName.Trim(), "[^a-zA-Z0-9]", string.Empty);
                }

                if (string.IsNullOrWhiteSpace(sanitized))
                {
                    sanitized = Guid.NewGuid().ToString("N");
                }

                // Ensure uniqueness of username
                string candidate = sanitized;
                var rnd = new Random();
                for (int i = 0; i < 10; i++)
                {
                    var found = await _userManager.FindByNameAsync(candidate);
                    if (found == null) break;
                    candidate = sanitized + rnd.Next(1000, 9999).ToString();
                }

                // CREATE USER WITH FirebaseUid IN THE CONSTRUCTOR/METHOD
                var user = User.Create(fullName, Enum.Parse<DAL.Enum.UserRole>(role), null, firebaseUid);

                // Set other properties
                user.Email = email;
                user.UserName = candidate;

                var result = await _userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    var errors = string.Join(";", result.Errors.Select(e => e.Description));
                    return Response<LoginResponseVM>.FailResponse($"User creation failed: {errors}");
                }

                // Create role if it doesn't exist
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole<Guid>(role));
                }

                await _userManager.AddToRoleAsync(user, role);

                // Generate token for the new user
                var token = _tokenService.GenerateToken(user.Id, role, user.FullName);

                var loginResponse = new LoginResponseVM
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
                        FirebaseUid = user.FirebaseUid // This will be set correctly
                    }
                };

                return Response<LoginResponseVM>.SuccessResponse(loginResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in RegisterAsync: {ex}");
                return Response<LoginResponseVM>.FailResponse($"Registration failed: {ex.Message}");
            }
        }
        // toggle between host and guest roles
        public async Task<Response<string>> ToggleUserRoleAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return Response<string>.FailResponse("User not found");
            var currentRoles = await _userManager.GetRolesAsync(user);
            string newRole;
            if (currentRoles.Contains("Host"))
            {
                newRole = "Guest";
            }
            else
            {
                newRole = "Host";
            }
            foreach (var r in currentRoles)
            {
                await _userManager.RemoveFromRoleAsync(user, r);
            }
            if (!await _roleManager.RoleExistsAsync(newRole))
                await _roleManager.CreateAsync(new IdentityRole<Guid>(newRole));
            await _userManager.AddToRoleAsync(user, newRole);
            var token = _tokenService.GenerateToken(user.Id, newRole, user.FullName);
            return Response<string>.SuccessResponse(token);
        }
        //make an admin
        public async Task<Response<string>> MakeUserAdminAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return Response<string>.FailResponse("User not found");
            var role = "Admin";
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole<Guid>(role));
            var currentRoles = await _userManager.GetRolesAsync(user);
            foreach (var r in currentRoles)
            {
                await _userManager.RemoveFromRoleAsync(user, r);
            }
            await _userManager.AddToRoleAsync(user, role);
            var token = _tokenService.GenerateToken(user.Id, role, user.FullName);
            return Response<string>.SuccessResponse(token);
        }

        public async Task<Response<LoginResponseVM>> LoginAsync(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return Response<LoginResponseVM>.FailResponse("Invalid credentials");
            var result = await _signInManager.CheckPasswordSignInAsync(user, password, false);
            if (!result.Succeeded) return Response<LoginResponseVM>.FailResponse("Invalid credentials");

            // determine role
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Guest";

            var token = _tokenService.GenerateToken(user.Id, role, user.FullName);
            
            // Build login response with onboarding status
            var loginResponse = new LoginResponseVM
            {
                Token = token,
                IsFirstLogin = user.IsFirstLogin,
                User = new UserInfoVM
                {
                    Id = user.Id,
                    Email = user.Email!,
                    UserName = user.UserName!,
                    FullName = user.FullName,
                    Role = role
                }
            };
            
            return Response<LoginResponseVM>.SuccessResponse(loginResponse);
        }

        public async Task<Response<bool>> SendPasswordResetAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return Response<bool>.FailResponse("User not found");
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            // INCOMPLETE: Email sending not implemented yet
            // TODO: send email with token
            return Response<bool>.SuccessResponse(true);
        }

        public async Task<Response<bool>> ResetPasswordAsync(string email, string token, string newPassword)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return Response<bool>.FailResponse("User not found");
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (!result.Succeeded) return Response<bool>.FailResponse(string.Join(";", result.Errors.Select(e => e.Description)));
            return Response<bool>.SuccessResponse(true);
        }
        public async Task<Response<LoginResponseVM>> OAuthLoginAsync(string provider, string externalToken)
        {
            if (provider.ToLower() != "google")
                return Response<LoginResponseVM>.FailResponse("Unsupported provider");

            GoogleJsonWebSignature.Payload payload;

            try
            {
                // Validate Google ID token
                payload = await GoogleJsonWebSignature.ValidateAsync(externalToken);
            }
            catch
            {
                return Response<LoginResponseVM>.FailResponse("Invalid Google token");
            }

            string email = payload.Email;
            string fullName = payload.Name ?? "User";
            string firebaseUid = payload.Subject; // Google UID
            string picture = payload.Picture;

            // Check if this user exists (by Firebase UID or email)
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUid || u.Email == email);

            if (user == null)
            {
                // Create username from email
                string baseName = email.Split('@')[0];
                string sanitized = Regex.Replace(baseName, "[^a-zA-Z0-9]", string.Empty);
                if (string.IsNullOrWhiteSpace(sanitized)) sanitized = Guid.NewGuid().ToString("N");

                // Ensure username is unique
                string candidate = sanitized;
                for (int i = 0; i < 10; i++)
                {
                    var exists = await _userManager.FindByNameAsync(candidate);
                    if (exists == null) break;
                    candidate = sanitized + new Random().Next(1000, 9999);
                }

                // Create user with no password
                user = User.Create(fullName, DAL.Enum.UserRole.Guest, picture, firebaseUid);
                user.Email = email;
                user.UserName = candidate;

                var createRes = await _userManager.CreateAsync(user);
                if (!createRes.Succeeded)
                    return Response<LoginResponseVM>.FailResponse(string.Join(";", createRes.Errors.Select(e => e.Description)));

                await _userManager.AddToRoleAsync(user, "Guest");
            }

            // Get role
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Guest";

            // Generate your JWT
            var token = _tokenService.GenerateToken(user.Id, role, user.FullName);

            return Response<LoginResponseVM>.SuccessResponse(new LoginResponseVM
            {
                Token = token,
                IsFirstLogin = user.IsFirstLogin,
                User = new UserInfoVM
                {
                    Id = user.Id,
                    Email = user.Email,
                    UserName = user.UserName!,
                    FullName = user.FullName,
                    Role = role
                }
            });
        }


        public async Task<Response<bool>> VerifyFaceIdAsync(Guid userId, string faceData)
        {
            // placeholder for face id verification
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return Response<bool>.FailResponse("User not found");
            // pretend verification succeeded
            return Response<bool>.SuccessResponse(true);
        }
        
        /// <summary>
        /// Mark user's onboarding as completed
        /// This is called after the user finishes the walkthrough guide
        /// </summary>
        public async Task<Response<bool>> CompleteOnboardingAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return Response<bool>.FailResponse("User not found");
            
            // Mark onboarding as completed using the entity method
            user.CompleteOnboarding();
            
            // Save changes to database
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded) 
                return Response<bool>.FailResponse("Failed to update onboarding status");
            
            return Response<bool>.SuccessResponse(true);
        }

        public async Task<User?> FindByEmailAsync(string email)
        {
            Console.WriteLine($"FindByEmailAsync called for: {email}");
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                Console.WriteLine($"User found: {user != null}");
                if (user != null)
                {
                    Console.WriteLine($"User ID: {user.Id}, FirebaseUid: {user.FirebaseUid}");
                }
                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in FindByEmailAsync: {ex}");
                return null;
            }
        }
        public async Task<IList<string>> GetRolesAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return new List<string>();
            return await _userManager.GetRolesAsync(user);
        }

        public string GenerateToken(Guid userId, string role, string fullName, Guid? orderId = null, Guid? listingId = null)
        {
            return _tokenService.GenerateToken(userId, role, fullName, orderId, listingId);
        }
    }
}
