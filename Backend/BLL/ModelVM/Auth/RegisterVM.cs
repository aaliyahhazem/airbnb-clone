namespace BLL.ModelVM.Auth
{
    public class RegisterVM
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string? FirebaseUid { get; set; }

    }
}