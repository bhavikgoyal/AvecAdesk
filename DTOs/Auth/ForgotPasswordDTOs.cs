namespace AvecADeskApi.DTOs.Auth
{
    public class ForgotPasswordRequestDTO
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordRequestDTO
    {
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}