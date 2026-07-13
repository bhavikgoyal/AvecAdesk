using AvecADeskApi.DTOs.Auth;
using AvecADeskApi.Helper;
using AvecADeskApi.Interfaces;
using AvecADeskApi.Model.Student;
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


        public AuthController(IAuthRepository repo, JwtTokenGenerator tokenGenerator, ILogger<AuthController> logger, IEmailService emailService)
        {
            _repo = repo;
            _tokenGenerator = tokenGenerator;
            _logger = logger;
            _emailService = emailService;
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

                var token = _tokenGenerator.GenerateToken(result.UserId, result.UserName);

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
    }
}