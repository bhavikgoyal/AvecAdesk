using AvecADeskApi.DTOs.Auth;
using AvecADeskApi.Helper;
using AvecADeskApi.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers
{
    [Authorize]
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _repo;
        private readonly IConfiguration _configuration;
        private readonly JwtTokenGenerator _tokenGenerator;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthRepository repo, IConfiguration configuration, ILogger<AuthController> logger)
        {
            _repo = repo;
            _configuration = configuration;
            _tokenGenerator = new JwtTokenGenerator(_configuration);
            _logger = logger;
        }

        // POST api/auth/login
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
        {
            try
            {
                if (request == null)
                    return BadRequest("Request body is required.");

                var user = await _repo.ValidateUserAsync(request.Email, request.Password);
                if (user == null)
                    return Unauthorized("Invalid credentials.");

                var token = _tokenGenerator.GenerateToken(user.UserId, user.UserName);

                return Ok(new
                {
                    Token = token,
                    User = user
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during login for email: {Email}", request?.Email);
                return StatusCode(500, "An error occurred while processing your login request.");
            }
        }

        // POST api/auth/vendor-login
        [AllowAnonymous]
        [HttpPost("vendor-login")]
        public async Task<IActionResult> VendorLogin([FromBody] VendorLoginDTO request)
        {
            try
            {
                if (request == null)
                    return BadRequest("Request body is required.");

                var result = !string.IsNullOrEmpty(request.VendorCode)
                    ? await _repo.ValidateVendorByCodeAsync(request.VendorCode)
                    : await _repo.ValidateVendorByPhoneAsync(request.Phone);

                if (result == null)
                    return NotFound("Vendor not found.");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during vendor login. VendorCode: {VendorCode}", request?.VendorCode);
                return StatusCode(500, "An error occurred while processing the vendor login request.");
            }
        }

        // POST api/auth/send-otp
        [AllowAnonymous]
        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] OTPRequestDTO request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Phone))
                    return BadRequest("Phone number is required.");

                var otp = await _repo.SendOtpAsync(request.Phone);

                if (otp == null)
                    return BadRequest("Failed to send OTP. Please try again.");

                return Ok(new { message = "OTP sent successfully.", otp = otp });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while sending OTP to phone: {Phone}", request?.Phone);
                return StatusCode(500, "An error occurred while sending the OTP.");
            }
        }

        // POST api/auth/verify-otp
        [AllowAnonymous]
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] OTPVerifyDTO request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Phone) || string.IsNullOrEmpty(request.Otp))
                    return BadRequest("Phone number and OTP are required.");

                var user = await _repo.VerifyOtpAndGetTokenAsync(request.Phone, request.Otp);

                if (user == null)
                    return Unauthorized("Invalid or expired OTP.");

                var token = _tokenGenerator.GenerateToken(user.UserId, user.UserName);

                return Ok(new
                {
                    Token = token,
                    User = user
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during OTP verification for phone: {Phone}", request?.Phone);
                return StatusCode(500, "An error occurred while verifying the OTP.");
            }
        }

        // POST api/auth/refresh-token
        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDTO request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.RefreshToken))
                    return BadRequest("Refresh token is required.");

                var isValid = await _repo.ValidateRefreshTokenAsync(request.RefreshToken);
                if (!isValid)
                    return Unauthorized("Invalid or expired refresh token.");

                var user = await _repo.GetUserByRefreshTokenAsync(request.RefreshToken);
                if (user == null)
                    return Unauthorized("User context not found.");

                var jwtGenerator = new JwtTokenGenerator(_configuration);
                var newAccessToken = jwtGenerator.GenerateToken(user.UserId, user.UserName);

                return Ok(new TokenResponseDTO
                {
                    Token = newAccessToken,
                    RefreshToken = request.RefreshToken,
                    Role = user.UserRoleId?.ToString() ?? "User",
                    Expiry = DateTime.UtcNow.AddMinutes(30)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during token refresh.");
                return StatusCode(500, "An error occurred while refreshing the token.");
            }
        }
    }
}