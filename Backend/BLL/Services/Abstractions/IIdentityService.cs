namespace BLL.Services.Abstractions
{
    public interface IIdentityService
    {
        Task<Response<LoginResponseVM>> RegisterAsync(string email, string password, string fullName, string userName, string? firebaseUid = null, string role = "Guest");
        Task<Response<LoginResponseVM>> LoginAsync(string email, string password);
        Task<Response<bool>> SendPasswordResetAsync(string email);
        Task<Response<bool>> ResetPasswordAsync(string email, string token, string newPassword);
        //Task<Response<string>> OAuthLoginAsync(string provider, string externalToken);
        Task<Response<LoginResponseVM>> OAuthLoginAsync(string provider, string externalToken);

        Task<Response<bool>> VerifyFaceIdAsync(Guid userId, string faceData);
        Task<Response<string>> ToggleUserRoleAsync(Guid userId);
        Task<Response<string>> MakeUserAdminAsync(Guid userId);
        Task<Response<bool>> CompleteOnboardingAsync(Guid userId);

        Task<User?> FindByEmailAsync(string email);
        Task<IList<string>> GetRolesAsync(Guid userId);



        string GenerateToken(Guid userId, string role, string fullName, Guid? orderId = null, Guid? listingId = null);
    }
}
