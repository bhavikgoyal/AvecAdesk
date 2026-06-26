using AvecADeskApi;
using AvecADeskApi.Helpers;
using AvecADeskApi.IRepository;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using AvecADeskApi.Helpers;
using AvecADeskApi.Model.EmployeeWorkHours;

namespace AvecADeskApi.Repository
{
    public class EmployeeWorkHoursRepositories : IRepository.IEmployeeWorkHoursRepository
    {
        private readonly SqlDbHelper _db;
        private readonly ILogger<EmployeeWorkHoursRepositories> _logger;

        public EmployeeWorkHoursRepositories( SqlDbHelper db, ILogger<EmployeeWorkHoursRepositories> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<int> InsertAsync(StartStop model)
        {
            try
            {
                var result = await _db.ExecuteScalarAsync( "sp_StartStop_Insert", cmd =>
                    {
                        cmd.Parameters.AddWithValue("@CardId", model.CardId);
                        cmd.Parameters.AddWithValue("@UserId", model.Userid);
                        cmd.Parameters.AddWithValue("@CardListItemId", model.CardListItemId);
                        cmd.Parameters.AddWithValue("@CardTitle", model.CardTitle);
                        cmd.Parameters.AddWithValue("@ItemName", model.ItemName);
                        cmd.Parameters.AddWithValue("@StartTime", model.StartTime);
                        cmd.Parameters.AddWithValue("@StopTime",
                            (object?)model.StopTime ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@MarkDone",
                            (object?)model.MarkDone ?? DBNull.Value);
                    });

                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to insert StartStop");

                throw;
            }
        }

        public async Task UpdateAsync(StartStop model)
        {
            try
            {
                await _db.ExecuteNonQueryAsync( "sp_StartStop_Update", cmd =>
                    {
                        cmd.Parameters.AddWithValue("@Id", model.Id);
                        cmd.Parameters.AddWithValue("@UserId", model.Userid);
                        cmd.Parameters.AddWithValue("@StopTime",
                            (object?)model.StopTime ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@MarkDone",
                            (object?)model.MarkDone ?? DBNull.Value);
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update StartStop");
                throw;
            }
        }
        public async Task<List<StartStop>> GetAllByUserIdAsync(int userId)
        {
            try
            {
                return await _db.ExecuteReaderListAsync( "sp_GetAllStartStopByUserId", cmd =>
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                    },
                    reader => new StartStop
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        CardId = reader["CardId"] == DBNull.Value ? null : Convert.ToInt32(reader["CardId"]),
                        Userid = Convert.ToInt32(reader["UserId"]),
                        CardListItemId = reader["CardListItemId"] == DBNull.Value ? null : Convert.ToInt32(reader["CardListItemId"]),
                        CardTitle = reader["CardTitle"] == DBNull.Value ? null : reader["CardTitle"].ToString(),
                        ItemName = reader["ItemName"] == DBNull.Value ? null : reader["ItemName"].ToString(),
                        StartTime = reader["StartTime"] == DBNull.Value ? null : Convert.ToDateTime(reader["StartTime"]),
                        StopTime = reader["StopTime"] == DBNull.Value ? null : Convert.ToDateTime(reader["StopTime"]),
                        MarkDone = reader["MarkDone"] == DBNull.Value ? null : Convert.ToBoolean(reader["MarkDone"])
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get StartStop by UserId");
                throw;
            }
        }

        public async Task<List<StartStop>> GetAllByUserGetallAsync()
        {
            try
            {
                return await _db.ExecuteReaderListAsync( "sp_GetAllStartStopBydata", cmd => { },
                    reader => new StartStop
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        CardId = reader["CardId"] == DBNull.Value ? null : Convert.ToInt32(reader["CardId"]),
                        CardListItemId = reader["CardListItemId"] == DBNull.Value ? null : Convert.ToInt32(reader["CardListItemId"]),
                        CardTitle = reader["CardTitle"] == DBNull.Value ? null : reader["CardTitle"].ToString(),
                        ItemName = reader["ItemName"] == DBNull.Value ? null : reader["ItemName"].ToString(),
                        StartTime = reader["StartTime"] == DBNull.Value ? null : Convert.ToDateTime(reader["StartTime"]),
                        StopTime = reader["StopTime"] == DBNull.Value ? null : Convert.ToDateTime(reader["StopTime"]),
                        MarkDone = reader["MarkDone"] == DBNull.Value ? null : Convert.ToBoolean(reader["MarkDone"]),
                        Userid = Convert.ToInt32(reader["UserId"]),
                        UserName = reader["UserName"] == DBNull.Value ? null : reader["UserName"].ToString()
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all StartStop records");
                throw;
            }
        }

        public async Task UpdateStopTimeForTodayAsync()
        {
            try
            {
                await _db.ExecuteNonQueryAsync( "sp_StartStop_Update_StopTime", cmd => { });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update StopTime");
                throw;
            }
        }

    }
}
