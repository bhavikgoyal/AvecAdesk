using AvecADeskApi.Model;
using AvecADeskApi.Interfaces;
using AvecADeskApi.Helpers;
using AvecADeskApi.LOG;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace AvecADeskApi.Repositories.UserRoles
{
    public class UserRoleRepository : IUserRoleRepository
    {
        private readonly SqlDbHelper _db;
        private readonly ILogger<UserRoleRepository> _logger;

        public UserRoleRepository(SqlDbHelper db, ILogger<UserRoleRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<UserRolesResponse>> GetRolesAsync()
        {
            try
            {
                _logger.LogInformation("GetRolesAsync called");

                var result = await _db.ExecuteReaderListAsync("dbo.sp_UserRoles_GetAll", cmd => { }, reader => new UserRolesResponse
                {
                    Id = reader["Id"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Id"]),
                    Name = reader["Name"] == DBNull.Value ? string.Empty : reader["Name"].ToString()
                });

                _logger.LogInformation("GetRolesAsync returned {Count} items", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetRolesAsync failed");
                throw;
            }
        }

        public async Task<List<UserRolesResponse>> GetCompaniesAsync()
        {
            try
            {
                _logger.LogInformation("GetCompaniesAsync called");

                var result = await _db.ExecuteReaderListAsync("dbo.sp_Companies_GetAll", cmd => { }, reader => new UserRolesResponse
                {
                    Id = reader["Id"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Id"]),
                    Name = reader["Name"] == DBNull.Value ? string.Empty : reader["Name"].ToString()
                });

                _logger.LogInformation("GetCompaniesAsync returned {Count} items", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetCompaniesAsync failed");
                throw;
            }
        }
    }
}
