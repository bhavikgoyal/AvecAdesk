using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace AvecADeskApi.Services
{
    public interface IPasswordResetTokenStore
    {
        string CreateToken(string email);
        string? ValidateAndConsumeToken(string token);
    }

    public class InMemoryPasswordResetTokenStore : IPasswordResetTokenStore
    {
        private readonly ConcurrentDictionary<string, (string Email, DateTime Expiry)> _tokens = new();

        public string CreateToken(string email)
        {
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            _tokens[token] = (email, DateTime.UtcNow.AddHours(1));
            return token;
        }

        public string? ValidateAndConsumeToken(string token)
        {
            if (_tokens.TryGetValue(token, out var entry) && entry.Expiry > DateTime.UtcNow)
            {
                _tokens.TryRemove(token, out _);
                return entry.Email;
            }
            _tokens.TryRemove(token, out _);
            return null;
        }
    }
}