namespace AvecADeskApi.DTOs.Auth
{
    public class OTPVerifyDTO
    {
        public string Phone { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
    }
}
