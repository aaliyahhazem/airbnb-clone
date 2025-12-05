namespace BLL.ModelVM.Auth
{
    /// <summary>
    /// Response returned after successful login
    /// Contains token, user info, and onboarding status
    /// </summary>
    public class LoginResponseVM
    {
        public string Token { get; set; } = null!;
        public bool IsFirstLogin { get; set; }
        public UserInfoVM User { get; set; } = null!;
    }

    /// <summary>
    /// Basic user information returned after login
    /// </summary>
    public class UserInfoVM
    {

        public Guid Id { get; set; }
        public string Email { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string? FirebaseUid { get; set; }

    }

    public class GoogleLoginDto
    {
        public string IdToken { get; set; } = string.Empty;
    }
}