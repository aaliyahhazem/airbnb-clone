namespace BLL.ModelVM.Email
{
    public class PasswordResetEmailVM
    {
        public string Email { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string ResetLink { get; set; } = null!;
        public DateTime ExpirationTime { get; set; }
    }

}
