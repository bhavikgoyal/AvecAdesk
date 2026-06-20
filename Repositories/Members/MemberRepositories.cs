using AvecADeskApi.Interfaces;
using AvecADeskApi.Helpers;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.UserResponse;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AvecADeskApi.Repositories.Members
{
    public class MembersRepository : IMembersRepository
    {
        private readonly SqlDbHelper _db;
        private readonly ILogger<MembersRepository> _logger;

        public MembersRepository(SqlDbHelper db, ILogger<MembersRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<UserResponse>> GetAllUsersAsync(int loginUserId, string roleName)
        {
            var result = new List<UserResponse>();

            try
            {
                _logger.LogInformation("GetAllUsersAsync called for LoginUserId:{LoginUserId} Role:{RoleName}", loginUserId, roleName);

                result = await _db.ExecuteReaderListAsync("sp_Users_GetAll",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@LoginUserID", loginUserId);
                        cmd.Parameters.AddWithValue("@RoleName", roleName);
                    },
                   reader => new UserResponse
                   {
                       UserId = SafeInt(reader["UserId"]),
                       UserName = SafeString(reader["UserName"]),
                       FirstName = SafeString(reader["FirstName"]),
                       LastName = SafeString(reader["LastName"]),
                       Email = SafeString(reader["Email"]),
                       PhoneNo = SafeString(reader["PhoneNo"]),
                       UserRoleId = SafeInt(reader["UserRoleId"]),
                       RoleName = SafeString(reader["RoleName"]),
                       CompaniesId = SafeInt(reader["CompaniesId"]),
                       IsActive = SafeBool(reader["IsActive"]),

                       AvatarBase64 = reader["Avatar"] == DBNull.Value
        ? null
        : SafeString(reader["Avatar"]),

                       CreatedOn = reader["CreatedOn"] == DBNull.Value
        ? (DateTime?)null
        : Convert.ToDateTime(reader["CreatedOn"])
                   });

                _logger.LogInformation("GetAllUsersAsync returned {Count} users for LoginUserId:{LoginUserId}", result.Count, loginUserId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllUsersAsync failed for LoginUserId:{LoginUserId}", loginUserId);
                throw;
            }
        }


        private bool SafeBool(object value)
        {
            return value != DBNull.Value && Convert.ToBoolean(value);
        }

        private int SafeInt(object value)
        {
            return value == DBNull.Value ? 0 : Convert.ToInt32(value);
        }

        private string SafeString(object value)
        {
            return value == DBNull.Value ? "" : value.ToString();
        }

        private byte[]? Base64ToBytes(string? base64)
        {
            if (string.IsNullOrWhiteSpace(base64))
                return null;

           
            if (base64.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return null;

            try
            {
                var idx = base64.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
                var payload = idx >= 0 ? base64.Substring(idx + 7) : base64;

                return Convert.FromBase64String(payload);
            }
            catch
            {
                return null;
            }
        }

        public async Task<int> CreateUserAsync(UserResponse request)
        {
            try
            {
                _logger.LogInformation("CreateUserAsync called for UserName:{UserName}", request?.UserName);

                var obj = await _db.ExecuteScalarAsync("dbo.sp_Users_Insert", cmd =>
                {
                    cmd.Parameters.AddWithValue("@UserName", request.UserName);
                    cmd.Parameters.AddWithValue("@FirstName", request.FirstName);
                    cmd.Parameters.AddWithValue("@LastName", request.LastName);
                    cmd.Parameters.AddWithValue("@Email", request.Email);
                    cmd.Parameters.AddWithValue("@PhoneNo", request.PhoneNo);
                    cmd.Parameters.AddWithValue("@UserRoleId", request.UserRoleId);
                    cmd.Parameters.AddWithValue("@CompaniesId", request.CompaniesId);
                    cmd.Parameters.AddWithValue("@Password", request.Password);
                    var avatarBytes = Base64ToBytes(request.AvatarBase64);
                    if (avatarBytes == null)
                        cmd.Parameters.AddWithValue("@Avatar", DBNull.Value);
                    else
                        cmd.Parameters.Add("@Avatar", SqlDbType.VarBinary, avatarBytes.Length).Value = avatarBytes;
                });

                var newUserId = Convert.ToInt32(obj);

                _logger.LogInformation("CreateUserAsync created UserId:{UserId} for UserName:{UserName}", newUserId, request.UserName);
                return newUserId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateUserAsync failed for UserName:{UserName}", request?.UserName);
                throw;
            }
        }

        public async Task<UserResponse?> GetUserByUserNameAsync(string userName)
        {
            try
            {
                return await _db.ExecuteReaderSingleAsync("dbo.sp_Users_GetByUserName", cmd =>
                {
                    cmd.Parameters.AddWithValue("@UserName", userName);
                }, reader => new UserResponse
                {
                    UserId = SafeInt(reader["UserId"]),
                    UserName = SafeString(reader["UserName"]),
                    FirstName = SafeString(reader["FirstName"]),
                    LastName = SafeString(reader["LastName"]),
                    Email = SafeString(reader["Email"]),
                    PhoneNo = SafeString(reader["PhoneNo"]),
                    UserRoleId = SafeInt(reader["UserRoleId"]),
                    CompaniesId = SafeInt(reader["CompaniesId"]),
                    IsActive = SafeBool(reader["IsActive"])
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetUserByUserNameAsync failed for UserName:{UserName}", userName);
                throw;
            }
        }

        public async Task<bool> UpdateUserAsync(UserResponse request)
        {
            try
            {
                _logger.LogInformation("UpdateUserAsync called for UserId:{UserId}", request?.UserId);

                var obj = await _db.ExecuteScalarAsync("dbo.sp_Users_Update", cmd =>
                {
                    cmd.Parameters.AddWithValue("@UserId", request.UserId);
                    cmd.Parameters.AddWithValue("@UserName", request.UserName);
                    cmd.Parameters.AddWithValue("@FirstName", request.FirstName);
                    cmd.Parameters.AddWithValue("@LastName", request.LastName);
                    cmd.Parameters.AddWithValue("@Email", request.Email);
                    cmd.Parameters.AddWithValue("@PhoneNo", request.PhoneNo);
                    cmd.Parameters.AddWithValue("@UserRoleId", request.UserRoleId);
                    cmd.Parameters.AddWithValue("@CompaniesId", request.CompaniesId);
                    cmd.Parameters.AddWithValue("@IsActive", request.IsActive);

                    var avatarBytes = Base64ToBytes(request.AvatarBase64);
                    if (avatarBytes == null)
                        cmd.Parameters.AddWithValue("@Avatar", DBNull.Value);
                    else
                        cmd.Parameters.Add("@Avatar", SqlDbType.VarBinary, avatarBytes.Length).Value = avatarBytes;
                });

                var result = Convert.ToInt32(obj);
                var success = result == 1;

                _logger.LogInformation("UpdateUserAsync completed for UserId:{UserId} Success:{Success}", request.UserId, success);
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateUserAsync failed for UserId:{UserId}", request?.UserId);
                throw;
            }
        }


        public async Task<bool> DeleteUserAsync(int userId)
        {
            try
            {
                _logger.LogInformation("DeleteUserAsync called for UserId:{UserId}", userId);

                var rows = await _db.ExecuteNonQueryWithResultAsync("dbo.sp_Users_Delete", cmd =>
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                });

                var success = rows > 0;

                _logger.LogInformation("DeleteUserAsync completed for UserId:{UserId} Success:{Success}", userId, success);
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteUserAsync failed for UserId:{UserId}", userId);
                throw;
            }
        }
        public async Task<bool> ResignMemberAsync(int userId, DateTime resignDate)
        {
            try
            {
                var result = await _db.ExecuteCommandAsync("sp_MemberResign", cmd =>
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@MemberResingOn", resignDate);

                    var returnParam = new SqlParameter
                    {
                        ParameterName = "@ReturnValue",
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.ReturnValue
                    };
                    cmd.Parameters.Add(returnParam);

                }, async cmd =>
                {
                    await cmd.ExecuteNonQueryAsync();
                    var val = cmd.Parameters["@ReturnValue"].Value;
                    return Convert.ToInt32(val);
                });

                return result == 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ResignMemberAsync failed for UserId:{UserId}", userId);
                throw;
            }
        }
    }
}