namespace AvecADeskApi.DTOs.Auth
{
    public class TokenResponseDTO
    {
        public string Token { get; set; }
        public string Role { get; set; }
        public DateTime Expiry { get; set; }
        public string RefreshToken { get; set; }
    }
}
