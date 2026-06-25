
using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Upload;
using AvecADeskApi.Model.UserActivity;
using AvecADeskApi.Model.UserPassword;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AvecADeskApi.Repositories.UserPassword;

public class UserPasswordRepository : IUserRepository
{
    private readonly SqlDbHelper _db;
    private readonly LogHelper _logHelper;

    public UserPasswordRepository(SqlDbHelper db, LogHelper logHelper)
    {
        _db = db;
        _logHelper = logHelper;
    }

    public async Task ChangePasswordAsync(UserChangePasswordRequest request)
    {
        try
        {
            await _db.ExecuteNonQueryAsync("dbo.sp_User_ChangePassword", cmd =>
            {
                cmd.Parameters.AddWithValue("@UserId", request.UserId);
                cmd.Parameters.AddWithValue("@OldPassword", request.OldPassword);
                cmd.Parameters.AddWithValue("@NewPassword", request.NewPassword);
            });
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(UserPasswordRepository)}.{nameof(ChangePasswordAsync)}", ex);
            throw;
        }
    }
}
