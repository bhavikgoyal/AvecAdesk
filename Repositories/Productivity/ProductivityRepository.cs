using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Productivity;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AvecADeskApi.Repositories.Productivity
{
    public class ProductivityRepository : IProductivityRepository
    {
        private readonly SqlDbHelper _db;
        private readonly LogHelper _logHelper;

        public ProductivityRepository(SqlDbHelper db, LogHelper logHelper)
        {
            _db = db;
            _logHelper = logHelper;
        }

        public async Task<ProductivityDashboardResponse> GetDashboardAsync(DateTime fromDate, DateTime toDate, int trendDays)
        {
            try
            {
                var response = await _db.ExecuteReaderCustomAsync(
                    "dbo.sp_ProductivityDashboard_Get",
                    cmd =>
                    {
                        cmd.Parameters.Add("@FromDate", SqlDbType.Date).Value = fromDate.Date;
                        cmd.Parameters.Add("@ToDate", SqlDbType.Date).Value = toDate.Date;
                        cmd.Parameters.Add("@TrendDays", SqlDbType.Int).Value = trendDays;
                    },
                    async reader =>
                    {
                        var result = new ProductivityDashboardResponse
                        {
                            FromDate = fromDate.ToString("yyyy-MM-dd"),
                            ToDate = toDate.ToString("yyyy-MM-dd"),
                        };

                        while (await reader.ReadAsync())
                        {
                            result.Members.Add(new ProductivityMemberResponse
                            {
                                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                                UserName = GetString(reader, "UserName"),
                                FullName = GetString(reader, "FullName"),
                                RoleName = GetString(reader, "RoleName"),
                                TotalSeconds = Convert.ToInt64(reader["TotalSeconds"]),
                                ProductiveSeconds = Convert.ToInt64(reader["ProductiveSeconds"]),
                                NeutralSeconds = Convert.ToInt64(reader["NeutralSeconds"]),
                                TasksTotal = Convert.ToInt32(reader["TasksTotal"]),
                                TasksDone = Convert.ToInt32(reader["TasksDone"]),
                                FirstLoginOn = GetDateTime(reader, "FirstLoginOn"),
                                LastSeenOn = GetDateTime(reader, "LastSeenOn"),
                                LoginDays = Convert.ToInt32(reader["LoginDays"]),
                            });
                        }

                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                result.Trend.Add(new ProductivityTrendPoint
                                {
                                    WorkDate = reader.GetDateTime(reader.GetOrdinal("WorkDate")).ToString("yyyy-MM-dd"),
                                    ProductiveSeconds = Convert.ToInt64(reader["ProductiveSeconds"]),
                                    NeutralSeconds = Convert.ToInt64(reader["NeutralSeconds"]),
                                });
                            }
                        }

                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                result.DailyActivity.Add(new ProductivityDailyActivity
                                {
                                    WorkDate = reader.GetDateTime(reader.GetOrdinal("WorkDate")).ToString("yyyy-MM-dd"),
                                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                                    UserName = GetString(reader, "UserName"),
                                    FullName = GetString(reader, "FullName"),
                                    TotalSeconds = Convert.ToInt64(reader["TotalSeconds"]),
                                    ProductiveSeconds = Convert.ToInt64(reader["ProductiveSeconds"]),
                                    NeutralSeconds = Convert.ToInt64(reader["NeutralSeconds"]),
                                });
                            }
                        }

                        return result;
                    });

                return response ?? new ProductivityDashboardResponse();
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(ProductivityRepository)}.{nameof(GetDashboardAsync)}", ex);
                throw;
            }
        }

        private static string? GetString(SqlDataReader reader, string column)
        {
            var value = reader[column];
            return value == DBNull.Value ? null : value.ToString();
        }

        private static DateTime? GetDateTime(SqlDataReader reader, string column)
        {
            var value = reader[column];
            return value == DBNull.Value ? null : Convert.ToDateTime(value);
        }
    }
}
