using AvecADeskApi.DTOs.Auth;
using AvecADeskApi.Helper;
using AvecADeskApi.Interfaces;
using AvecADeskApi.Model.Student;
using AvecADeskApi.Services;
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
        private readonly JwtTokenGenerator _tokenGenerator;
        private readonly ILogger<AuthController> _logger;
        private readonly IEmailService _emailService;
        private readonly IPasswordResetTokenStore _tokenStore;
        private readonly IConfiguration _configuration;

        public AuthController(IAuthRepository repo, JwtTokenGenerator tokenGenerator, ILogger<AuthController> logger, IEmailService emailService, IPasswordResetTokenStore tokenStore,
    IConfiguration configuration)
        {
            _repo = repo;
            _tokenGenerator = tokenGenerator;
            _logger = logger;
            _emailService = emailService;
            _tokenStore = tokenStore;
            _configuration = configuration;
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

                if (string.IsNullOrWhiteSpace(request.VendorCode) && string.IsNullOrWhiteSpace(request.Phone))
                    return BadRequest("Vendor code or phone number is required.");

                var result = !string.IsNullOrWhiteSpace(request.VendorCode)
                    ? await _repo.ValidateVendorByCodeAsync(request.VendorCode.Trim())
                    : await _repo.ValidateVendorByPhoneAsync(request.Phone!.Trim());

                if (result == null)
                    return NotFound("Vendor not found.");

                var token = _tokenGenerator.GenerateToken(result.UserId, result.UserName, result.VendorId);

                return Ok(new
                {
                    Token = token,
                    User = result
                });
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

                var token = _tokenGenerator.GenerateToken(user.UserId, user.UserName, user.VendorId);

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

                var newAccessToken = _tokenGenerator.GenerateToken(user.UserId, user.UserName);

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

        // POST api/auth/register
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] StudentRegisterRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest("Request body is required.");

                var result = await _repo.RegisterStudentAsync(request);

                if (!result.Success)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = result.Message
                    });
                }

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendVerificationEmailAsync(
                            request.Email,
                            result.VerificationCode);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Failed to send verification email to {Email}",
                            request.Email);
                    }
                });

                return Ok(new
                {
                    Success = true,
                    Message = "Verification code sent successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during student registration.");
                return StatusCode(500, ex.Message);
            }
        }

        // POST api/auth/verify-email
        [AllowAnonymous]
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
        {
            try
            {
                var result = await _repo.VerifyEmailAsync(request);

                if (!result)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Invalid verification code."
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Message = "Email verified successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during email verification.");

                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Something went wrong."
                });
            }
        }

        [AllowAnonymous]
        [HttpPost("Studentlogin")]
        public async Task<IActionResult> Studentlogin([FromBody] LoginRequestDTO request)
        {
            try
            {
                if (request == null)
                    return BadRequest("Request body is required.");

                var Student = await _repo.StudentloginAsync(request.Email, request.Password);

                if (Student == null)
                    return Unauthorized("Invalid credentials.");

                var token = _tokenGenerator.StudentGenerateToken(
                    Student.Id,
                    $"{Student.FirstName} {Student.LastName}"
                );

                return Ok(new
                {
                    Token = token,
                    Student = Student
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during login for email: {Email}", request?.Email);
                return StatusCode(500, "An error occurred while processing your login request.");
            }
        }

        // POST api/auth/forgot-password
        [AllowAnonymous]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDTO request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Email))
                    return BadRequest("Email is required.");

                var email = request.Email.Trim();
                var exists = await _repo.UserExistsByEmailAsync(email);

               
                if (exists)
                {
                    var token = _tokenStore.CreateToken(email);
                    var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:3000";
                    var resetLink = $"{frontendBaseUrl}/reset-password?token={Uri.EscapeDataString(token)}";

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _emailService.SendPasswordResetEmailAsync(email, resetLink);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send password reset email to {Email}", email);
                        }
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Message = "If an account with that email exists, a reset link has been sent."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during forgot-password for email: {Email}", request?.Email);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        // POST api/auth/reset-password
        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDTO request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
                    return BadRequest("Token and new password are required.");

                if (request.NewPassword != request.ConfirmPassword)
                    return BadRequest("Passwords do not match.");

                if (request.NewPassword.Length > 50)
                    return BadRequest("Password must be 50 characters or fewer.");

                var email = _tokenStore.ValidateAndConsumeToken(request.Token);
                if (email == null)
                    return BadRequest(new { Success = false, Message = "Invalid or expired reset link." });

                var updated = await _repo.UpdatePasswordByEmailAsync(email, request.NewPassword);
                if (!updated)
                    return BadRequest(new { Success = false, Message = "Unable to reset password." });

                return Ok(new { Success = true, Message = "Password has been reset successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during password reset.");
                return StatusCode(500, "An error occurred while resetting your password.");
            }
        }
    }
}