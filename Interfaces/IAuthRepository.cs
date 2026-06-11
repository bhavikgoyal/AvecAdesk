using AvecADeskApi.DTOs.Auth;
using AvecADeskApi.Model;

namespace AvecADeskApi.Interfaces;

public interface IAuthRepository
{
    Task<UserLoginDTO?> ValidateUserAsync(string email, string password);
    Task<User?> GetUserByPhoneAsync(string phone);
    Task SaveRefreshTokenAsync(int userId, string refreshToken);
    Task<bool> ValidateRefreshTokenAsync(string refreshToken);
    Task<User?> GetUserByRefreshTokenAsync(string refreshToken);
}