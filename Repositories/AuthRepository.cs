using AvecADeskApi.Data;
using AvecADeskApi.DTOs.Auth;
using AvecADeskApi.Interfaces;
using AvecADeskApi.Model;
using Microsoft.EntityFrameworkCore;

namespace AvecADeskApi.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly AppDbContext _context;

        public AuthRepository(AppDbContext context)
        {
            _context = context;
        }

        // ================= VALIDATE USER (SP LOGIN) =================
        public async Task<UserLoginDTO?> ValidateUserAsync(string email, string password)
        {
            // AsNoTracking aur FromSqlRaw database se result layenge
            var result = _context.UserLoginDTOs
                .FromSqlRaw("EXEC sp_ValidateUser @Email={0}, @Password={1}", email, password)
                .AsNoTracking()
                .AsEnumerable(); // Ab ye list memory mein aa gayi

            // Memory mein se pehla item nikalne ke liye sirf .FirstOrDefault() use karein
            return await Task.FromResult(result.FirstOrDefault());
        }
        // ================= GET USER BY PHONE =================
        public async Task<User?> GetUserByPhoneAsync(string phone)
        {
            return await _context.Users
                .FirstOrDefaultAsync(x => x.PhoneNo == phone);
        }

        // ================= SAVE REFRESH TOKEN =================
        public async Task SaveRefreshTokenAsync(int userId, string refreshToken)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user != null)
            {
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

                await _context.SaveChangesAsync();
            }
        }

        // ================= VALIDATE REFRESH TOKEN =================
        public async Task<bool> ValidateRefreshTokenAsync(string refreshToken)
        {
            return await _context.Users.AnyAsync(x =>
                x.RefreshToken == refreshToken &&
                x.RefreshTokenExpiry > DateTime.UtcNow);
        }

        // ================= GET USER BY REFRESH TOKEN =================
        public async Task<User?> GetUserByRefreshTokenAsync(string refreshToken)
        {
            return await _context.Users
                .FirstOrDefaultAsync(x => x.RefreshToken == refreshToken);
        }

    }
}