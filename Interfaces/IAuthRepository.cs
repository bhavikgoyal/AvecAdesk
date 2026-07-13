using AvecADeskApi.DTOs.Auth;
using AvecADeskApi.Model;
using AvecADeskApi.Model.Student;

namespace AvecADeskApi.Interfaces
{
    public interface IAuthRepository
    {
        Task<UserLoginDTO?> ValidateUserAsync(string email, string password);
        Task<UserLoginResult?> ValidateVendorByCodeAsync(string vendorCode);
        Task<UserLoginResult?> ValidateVendorByPhoneAsync(string phone);

        
        Task<string?> SendOtpAsync(string phone);
        Task<UserLoginResult?> VerifyOtpAndGetTokenAsync(string phone, string otp);

        Task<User?> GetUserByRefreshTokenAsync(string refreshToken);
        Task<bool> ValidateRefreshTokenAsync(string refreshToken);

        Task<RegisterStudentResult> RegisterStudentAsync(StudentRegisterRequest request);
        Task<bool> VerifyEmailAsync(VerifyEmailRequest request);
        Task<StudentLoginDTO?> StudentloginAsync(string email, string password);
    }
}