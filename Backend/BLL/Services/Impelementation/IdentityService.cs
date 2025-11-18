using BLL.ModelVM.Response;
using BLL.Services.Abstractions;
using DAL.Entities;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System;

namespace BLL.Services.Impelementation
{
 public class IdentityService : IIdentityService
 {
 private readonly UserManager<User> _userManager;
 private readonly SignInManager<User> _signInManager;
 private readonly RoleManager<IdentityRole<Guid>> _roleManager;
 private readonly ITokenService _tokenService;

 public IdentityService(UserManager<User> userManager, SignInManager<User> signInManager, RoleManager<IdentityRole<Guid>> roleManager, ITokenService tokenService)
 {
 _userManager = userManager;
 _signInManager = signInManager;
 _roleManager = roleManager;
 _tokenService = tokenService;
 }

 public async Task<Response<string>> RegisterAsync(string email, string password, string fullName, string? firebaseUid = null, string role = "Guest")
 {
 var existing = await _userManager.FindByEmailAsync(email);
 if (existing != null) return Response<string>.FailResponse("Email already registered");

 var user = User.Create(fullName, Enum.Parse<DAL.Enum.UserRole>(role));
 user.Email = email;
 user.UserName = email;
 var result = await _userManager.CreateAsync(user, password);
 if (!result.Succeeded) return Response<string>.FailResponse(string.Join(";", result.Errors.Select(e => e.Description)));

 if (!await _roleManager.RoleExistsAsync(role))
 await _roleManager.CreateAsync(new IdentityRole<Guid>(role));

 await _userManager.AddToRoleAsync(user, role);

 // generate token for the new user
 var token = _tokenService.GenerateToken(user.Id, role);
 return Response<string>.SuccessResponse(token);
 }

 public async Task<Response<string>> LoginAsync(string email, string password)
 {
 var user = await _userManager.FindByEmailAsync(email);
 if (user == null) return Response<string>.FailResponse("Invalid credentials");
 var result = await _signInManager.CheckPasswordSignInAsync(user, password, false);
 if (!result.Succeeded) return Response<string>.FailResponse("Invalid credentials");

 // determine role
 var roles = await _userManager.GetRolesAsync(user);
 var role = roles.FirstOrDefault() ?? "Guest";

 var token = _tokenService.GenerateToken(user.Id, role);
 return Response<string>.SuccessResponse(token);
 }

 public async Task<Response<bool>> SendPasswordResetAsync(string email)
 {
 var user = await _userManager.FindByEmailAsync(email);
 if (user == null) return Response<bool>.FailResponse("User not found");
 var token = await _userManager.GeneratePasswordResetTokenAsync(user);
 // TODO: send email with token
 return Response<bool>.SuccessResponse(true);
 }

 public async Task<Response<bool>> ResetPasswordAsync(string email, string token, string newPassword)
 {
 var user = await _userManager.FindByEmailAsync(email);
 if (user == null) return Response<bool>.FailResponse("User not found");
 var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
 if (!result.Succeeded) return Response<bool>.FailResponse(string.Join(";", result.Errors.Select(e=>e.Description)));
 return Response<bool>.SuccessResponse(true);
 }

 public async Task<Response<string>> OAuthLoginAsync(string provider, string externalToken)
 {
 // Very simplified: in real app validate token with provider
 // Assume externalToken contains email
 var email = externalToken;
 var user = await _userManager.FindByEmailAsync(email);
 if (user == null)
 {
 user = User.Create(email.Split('@')[0]);
 user.Email = email;
 user.UserName = email;
 await _userManager.CreateAsync(user);
 await _userManager.AddToRoleAsync(user, "Guest");
 }

 var roles = await _userManager.GetRolesAsync(user);
 var role = roles.FirstOrDefault() ?? "Guest";
 var token = _tokenService.GenerateToken(user.Id, role);
 return Response<string>.SuccessResponse(token);
 }

 public async Task<Response<bool>> VerifyFaceIdAsync(Guid userId, string faceData)
 {
 // placeholder for face id verification
 var user = await _userManager.FindByIdAsync(userId.ToString());
 if (user == null) return Response<bool>.FailResponse("User not found");
 // pretend verification succeeded
 return Response<bool>.SuccessResponse(true);
 }

 public string GenerateToken(Guid userId, string role, Guid? orderId = null, Guid? listingId = null)
 {
 return _tokenService.GenerateToken(userId, role, orderId, listingId);
 }
 }
}
