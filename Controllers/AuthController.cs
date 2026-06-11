using AvecADeskApi.DTOs.Auth;
using AvecADeskApi.Interfaces;  
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDTO request)
        {
            return Ok(await _authService.LoginAsync(request));
        }

        [HttpPost("vendor-login")]
        public async Task<IActionResult> VendorLogin(VendorLoginDTO request)
        {
            return Ok(await _authService.VendorLoginAsync(request));
        }

        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp(OTPRequestDTO request)
        {
            return Ok(await _authService.SendOtpAsync(request));
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp(OTPVerifyDTO request)
        {
            return Ok(await _authService.VerifyOtpAsync(request));
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(string refreshToken)
        {
            return Ok(await _authService.RefreshTokenAsync(refreshToken));
        }
    }
}