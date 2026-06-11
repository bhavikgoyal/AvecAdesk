using AvecADeskApi.DTOs.Auth;
using AvecADeskApi.Interfaces;
using AvecADeskApi.Services.Helpers;
using System.Security.Cryptography;

namespace AvecADeskApi.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _repo;
        private readonly JwtTokenGenerator _jwt;

        public AuthService(IAuthRepository repo, JwtTokenGenerator jwt)
        {
            _repo = repo;
            _jwt = jwt;
        }

        // ================= LOGIN =================
        public async Task<TokenResponseDTO> LoginAsync(LoginRequestDTO request)
        {
            var user = await _repo.ValidateUserAsync(request.Email, request.Password);

            if (user == null)
                throw new Exception("Invalid credentials");

            var token = _jwt.GenerateToken(user.Email, user.UserRoleId.ToString());

            var refreshToken = GenerateRefreshToken();
            await _repo.SaveRefreshTokenAsync(user.UserId, refreshToken);

            return new TokenResponseDTO
            {
                Token = token,
                Role = user.UserRoleId.ToString(),
                RefreshToken = refreshToken,
                Expiry = DateTime.UtcNow.AddMinutes(60)
            };
        }

        // ================= VENDOR LOGIN =================
        public async Task<TokenResponseDTO> VendorLoginAsync(VendorLoginDTO request)
        {
            var user = await _repo.GetUserByPhoneAsync(request.Phone);

            if (user == null)
                throw new Exception("Vendor not found");

            var token = _jwt.GenerateToken(user.Email, user.UserRoleId.ToString());

            return new TokenResponseDTO
            {
                Token = token,
                Role = user.UserRoleId.ToString(),
                Expiry = DateTime.UtcNow.AddMinutes(60)
            };
        }

        // ================= SEND OTP =================
        public async Task<string> SendOtpAsync(OTPRequestDTO request)
        {
            var otp = new Random().Next(100000, 999999).ToString();

            // TODO: Save OTP in DB / Redis (recommended)
            // TODO: Send SMS via provider (Fast2SMS / Twilio)

            return $"OTP sent to {request.Phone}: {otp}";
        }

        // ================= VERIFY OTP =================
        public async Task<TokenResponseDTO> VerifyOtpAsync(OTPVerifyDTO request)
        {
            // TODO: Validate OTP from DB / cache

            var user = await _repo.GetUserByPhoneAsync(request.Phone);

            if (user == null)
                throw new Exception("User not found");

            var token = _jwt.GenerateToken(user.Email, user.UserRoleId.ToString());

            return new TokenResponseDTO
            {
                Token = token,
                Role = user.UserRoleId.ToString(),
                Expiry = DateTime.UtcNow.AddMinutes(60)
            };
        }

        // ================= REFRESH TOKEN =================
        public async Task<TokenResponseDTO> RefreshTokenAsync(string refreshToken)
        {
            var isValid = await _repo.ValidateRefreshTokenAsync(refreshToken);

            if (!isValid)
                throw new Exception("Invalid refresh token");

            // In real system: fetch user from DB using refresh token
            var user = await _repo.GetUserByRefreshTokenAsync(refreshToken);

            if (user == null)
                throw new Exception("User not found");

            var newToken = _jwt.GenerateToken(user.Email, user.UserRoleId.ToString());

            return new TokenResponseDTO
            {
                Token = newToken,
                Role = user.UserRoleId.ToString(),
                Expiry = DateTime.UtcNow.AddMinutes(60)
            };
        }

        // ================= HELPERS =================
        private string GenerateRefreshToken()
        {
            var bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
}