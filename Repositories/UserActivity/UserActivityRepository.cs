using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.UserActivity;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AvecADeskApi.Repositories.UserActivity
{
    public class UserActivityRepository: IUserActivityRepository
    {
        private readonly SqlDbHelper _db;
        private readonly LogHelper _logHelper;

        public UserActivityRepository(SqlDbHelper db, LogHelper logHelper)
        {
            _db = db;
            _logHelper = logHelper;
        }

        public async Task<List<UserActivityResponse>> GetWorkReportAsync(
       DateTime fromDate,
       DateTime toDate,
       string? employeeName = null)
        {
            try
            {
                return await _db.ExecuteReaderCustomAsync(
                    "sp_ActivityReport_Get",
                    cmd =>
                    {
                        cmd.Parameters.Add("@FromDate", SqlDbType.Date).Value = fromDate.Date;
                        cmd.Parameters.Add("@ToDate", SqlDbType.Date).Value = toDate.Date;

                        cmd.Parameters.Add("@EmployeeName", SqlDbType.NVarChar, 100).Value =
                            string.IsNullOrWhiteSpace(employeeName)
                                ? DBNull.Value
                                : employeeName;
                    },
                    async reader =>
                    {
                        var list = new List<UserActivityResponse>();

                        while (await reader.ReadAsync())
                        {
                            string? totalTimeStr = null;
                            string? idleTimeStr = null;
                            string? activeTimeStr = null;

                            var totalTimeVal = GetValue(reader, "TotalTime");
                            var idleTimeVal = GetValue(reader, "IdleTime");
                            var activeTimeVal = GetValue(reader, "ActiveTime");

                            if (totalTimeVal != null)
                                totalTimeStr = totalTimeVal.ToString();

                            if (idleTimeVal != null)
                                idleTimeStr = idleTimeVal.ToString();

                            if (activeTimeVal != null)
                                activeTimeStr = activeTimeVal.ToString();

                            var totalSecondsVal = GetValue(reader, "TotalSeconds");
                            var idleSecondsVal = GetValue(reader, "IdleSeconds");

                            if (totalTimeStr == null && totalSecondsVal != null)
                            {
                                totalTimeStr = SecondsToHms(Convert.ToInt64(totalSecondsVal));
                            }

                            if (idleTimeStr == null && idleSecondsVal != null)
                            {
                                idleTimeStr = SecondsToHms(Convert.ToInt64(idleSecondsVal));
                            }

                            if (activeTimeStr == null && totalSecondsVal != null)
                            {
                                long totalSeconds = Convert.ToInt64(totalSecondsVal);
                                long idleSeconds = idleSecondsVal != null
                                    ? Convert.ToInt64(idleSecondsVal)
                                    : 0;

                                activeTimeStr = SecondsToHms(
                                    Math.Max(totalSeconds - idleSeconds, 0));
                            }

                            object? uidVal =
                                GetValue(reader, "UserId") ??
                                GetValue(reader, "userid");

                            object? unameVal =
                                GetValue(reader, "UserName") ??
                                GetValue(reader, "username");

                            object? workDateVal =
                                GetValue(reader, "WorkDate") ??
                                GetValue(reader, "workdate");

                            int userId = 0;

                            if (uidVal != null)
                            {
                                int.TryParse(uidVal.ToString(), out userId);
                            }

                            string? workDate = null;

                            if (workDateVal != null)
                            {
                                if (DateTime.TryParse(workDateVal.ToString(), out var dt))
                                {
                                    workDate = dt.ToString("yyyy-MM-dd");
                                }
                            }

                            list.Add(new UserActivityResponse
                            {
                                UserId = userId,
                                UserName = unameVal?.ToString(),
                                WorkDate = workDate,
                                TotalTime = totalTimeStr,
                                Productive = activeTimeStr,
                                Neutral = idleTimeStr,
                                Avatar = GetValue(reader, "Avatar")?.ToString(),
                                AvatarBinary = GetValue(reader, "AvatarBinary") as byte[],
                            });
                        }

                        return list;
                    })
                    ?? new List<UserActivityResponse>();
            }
            catch (Exception ex)
            {
                _logHelper.LogError(nameof(GetWorkReportAsync), ex);
                return new List<UserActivityResponse>();
            }
        }

        private static object? GetValue(IDataRecord record, string columnName)
        {
            for (int i = 0; i < record.FieldCount; i++)
            {
                if (string.Equals(
                    record.GetName(i),
                    columnName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    var value = record.GetValue(i);

                    if (value == DBNull.Value || value == null)
                        return null;

                    if (value is string str && string.IsNullOrWhiteSpace(str))
                        return null;

                    return value;
                }
            }
            return null;
        }
        private static string SecondsToHms(long seconds)
        {
            var time = TimeSpan.FromSeconds(Math.Max(seconds, 0));
            return time.ToString(@"hh\:mm\:ss");
        }
    }
}