using AvecADeskApi.DTOs.Auth;
using AvecADeskApi.Interfaces;
using AvecADeskApi.Model;
using AvecADeskApi.Model.Student;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AvecADeskApi.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly string _connectionString;

        public AuthRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new Exception("Connection string not found in appsettings.json");
        }

        public async Task<UserLoginDTO?> ValidateUserAsync(string email, string password)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_ValidateUser", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@Password", password);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new UserLoginDTO
                {
                    UserId = (int)reader["UserId"],
                    UserName = reader["UserName"].ToString() ?? "",
                  
                    Email = reader["Email"] != DBNull.Value ? reader["Email"].ToString()! : string.Empty,
                    UserRoleId = (int)reader["UserRoleId"],
                    UserRoleName = reader["UserRoleName"]?.ToString() ?? string.Empty
                };
            }
            return null;
        }

        
        public async Task<User?> GetUserByPhoneAsync(string phone)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_GetUserByPhone", conn); 
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Phone", phone);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new User { UserId = (int)reader["UserId"], PhoneNo = reader["PhoneNo"].ToString() };
            }
            return null;
        }

        
        public async Task SaveRefreshTokenAsync(int userId, string refreshToken)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_SaveRefreshToken", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@RefreshToken", refreshToken);
            cmd.Parameters.AddWithValue("@Expiry", DateTime.UtcNow.AddDays(7));

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        
        public async Task<bool> ValidateRefreshTokenAsync(string refreshToken)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_ValidateRefreshToken", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@RefreshToken", refreshToken);

            await conn.OpenAsync();
            var result = await cmd.ExecuteScalarAsync();

            return result != null && Convert.ToInt32(result) == 1;
        }

       
        public async Task<User?> GetUserByRefreshTokenAsync(string refreshToken)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_GetUserByRefreshToken", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@RefreshToken", refreshToken);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new User { UserId = (int)reader["UserId"], UserName = reader["UserName"].ToString() ?? "" };
            }
            return null;
        }

        public async Task<UserLoginResult?> ValidateVendorAsync(string phone)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_ValidateVendor", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Phone", phone);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new UserLoginResult { UserId = (int)reader["UserId"], UserName = reader["UserName"].ToString() ?? "" };
            }
            return null;
        }

        public async Task<UserLoginResult?> ValidateVendorByCodeAsync(string vendorCode)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_ValidateVendorByCode", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Code", vendorCode);

            await conn.OpenAsync();
            UserLoginResult? result = null;
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    result = MapReaderToUserLoginResult(reader);
                    result.IsActive = true;
                }
            }

            // VendorId must come from the login-matched vendor row (code/phone),
            // NOT from UserId lookup (multiple vendors can share one UserId).
            if (result != null && result.VendorId is null or <= 0)
                result.VendorId = await GetVendorIdByUserIdAsync(result.UserId);

            return result;
        }
        public async Task<UserLoginResult?> ValidateVendorByPhoneAsync(string phone)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_ValidateVendorByPhone", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Phone", phone);

            await conn.OpenAsync();
            UserLoginResult? result = null;
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    result = MapReaderToUserLoginResult(reader);
                }
            }

            if (result != null && result.VendorId is null or <= 0)
                result.VendorId = await GetVendorIdByUserIdAsync(result.UserId);

            return result;
        }
        
        public async Task<string?> SendOtpAsync(string phone)
        {
            
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_SendOtp", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Phone", phone);
            var otpParam = new SqlParameter("@GeneratedOtp", SqlDbType.NVarChar, 6)
            { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(otpParam);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
            string otp = otpParam.Value?.ToString();

           
            if (!string.IsNullOrEmpty(otp))
            {
                await SendSmsToMobile(phone, otp);
            }

            return otp;
        }

        private async Task SendSmsToMobile(string phone, string otp)
        {
            try
            {
                using var client = new HttpClient();
               
                string apiUrl = $"https://api.actual-sms-provider.com/send?key=YOUR_REAL_KEY&phone={phone}&otp={otp}";
                await client.GetAsync(apiUrl);
            }
            catch (Exception ex)
            {
                
                System.Diagnostics.Debug.WriteLine($"SMS Error: {ex.Message}");
            }
        }
       
        public async Task<UserLoginResult?> VerifyOtpAndGetTokenAsync(string phone, string otp)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_VerifyOtp", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Phone", phone);
            cmd.Parameters.AddWithValue("@OtpCode", otp);

            await conn.OpenAsync();
            UserLoginResult? user = null;
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync() && reader["UserId"] != DBNull.Value)
                    user = MapReaderToUserLoginResult(reader);
            }

            if (user != null && user.VendorId is null or <= 0)
                user.VendorId = await GetVendorIdByUserIdAsync(user.UserId);

            return user;
        }
        private UserLoginResult MapReaderToUserLoginResult(SqlDataReader reader)
        {
            int? vendorId = null;
            for (var i = 0; i < reader.FieldCount; i++)
            {
                if (string.Equals(reader.GetName(i), "VendorId", StringComparison.OrdinalIgnoreCase)
                    && reader["VendorId"] != DBNull.Value)
                {
                    vendorId = Convert.ToInt32(reader["VendorId"]);
                    break;
                }
            }

            return new UserLoginResult
            {
                UserId = Convert.ToInt32(reader["UserId"]),
                UserName = reader["UserName"]?.ToString() ?? "",
                Email = reader["Email"] != DBNull.Value ? reader["Email"].ToString() : null,
                UserRoleId = reader["UserRoleId"] != DBNull.Value ? Convert.ToInt32(reader["UserRoleId"]) : 0,
                VendorId = vendorId
            };
        }

        private async Task<int?> GetVendorIdByUserIdAsync(int userId)
        {
            if (userId <= 0) return null;

            await using var conn = new SqlConnection(_connectionString);
            await using var cmd = new SqlCommand("sp_GetVendorIdByUserId", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@UserId", userId);
            await conn.OpenAsync();
            var result = await cmd.ExecuteScalarAsync();
            return result == null || result == DBNull.Value ? null : Convert.ToInt32(result);
        }

        public async Task<RegisterStudentResult> RegisterStudentAsync(StudentRegisterRequest request)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_RegisterStudent", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            string verificationCode = new Random().Next(100000, 999999).ToString();

            cmd.Parameters.AddWithValue("@FirstName", request.FirstName);
            cmd.Parameters.AddWithValue("@LastName", request.LastName);
            cmd.Parameters.AddWithValue("@Email", request.Email);
            cmd.Parameters.AddWithValue("@Phone", request.Phone);
            cmd.Parameters.AddWithValue("@Password", request.Password);
            cmd.Parameters.AddWithValue("@VerificationCode", verificationCode);

            await conn.OpenAsync();

            int result = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            if (result == 0)
            {
                return new RegisterStudentResult
                {
                    Success = false,
                    Message = "Email already exists."
                };
            }

            return new RegisterStudentResult
            {
                Success = true,
                Message = "Student registered successfully.",
                VerificationCode = verificationCode
            };
        }

        public async Task<bool> VerifyEmailAsync(VerifyEmailRequest request)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_VerifyEmail", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Email", request.Email);
            cmd.Parameters.AddWithValue("@VerificationCode", request.VerificationCode);

            await conn.OpenAsync();

            int result = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            return result == 1;
        }
        public async Task<StudentLoginDTO?> StudentloginAsync(string email, string password)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("Sp_StudentApplicationDetailsLogin", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@Password", password);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new StudentLoginDTO
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    FirstName = reader["FirstName"]?.ToString() ?? "",
                    LastName = reader["LastName"]?.ToString() ?? "",
                    Email = reader["Email"]?.ToString() ?? "",
                    Password = reader["Password"]?.ToString() ?? "",
                    MobileNumber = reader["MobileNumber"]?.ToString() ?? ""
                };
            }
            return null;
        }
    }
}