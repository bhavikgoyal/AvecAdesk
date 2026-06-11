using AvecADeskApi.DTOs.Auth;

namespace AvecADeskApi.Interfaces;

public interface IAuthService
{
    Task<TokenResponseDTO> LoginAsync(LoginRequestDTO request);

    Task<TokenResponseDTO> VendorLoginAsync(VendorLoginDTO request);

    Task<string> SendOtpAsync(OTPRequestDTO request);

    Task<TokenResponseDTO> VerifyOtpAsync(OTPVerifyDTO request);

    Task<TokenResponseDTO> RefreshTokenAsync(string refreshToken);
}