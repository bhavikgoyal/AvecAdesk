using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model;
using AvecADeskApi.Model.UserActivity;
using AvecADeskApi.Model.ViewActivityHistory;
using System.Data;
using System.Text.Json;

namespace AvecADeskApi.Repositories
{
    public class ViewActivityHistoryRepository : IViewActivityHistoryRepository
    {
        private readonly SqlDbHelper _db;
        private readonly LogHelper _logHelper;

        public ViewActivityHistoryRepository(SqlDbHelper db, LogHelper logHelper)
        {
            _db = db;
            _logHelper = logHelper;
        }

        public async Task<List<ViewActivityHistoryResponse>>GetActivityHistoryByUserAsync(int userId,DateTime date)
        {
            try
            {
                return await _db.ExecuteReaderCustomAsync(
                    "ActivityHistoryByUser",
                    cmd =>
                    {
                        cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                        cmd.Parameters.Add("@TargetDate", SqlDbType.Date).Value = date.Date;
                    },
                    async reader =>
                    {
                        var list = new List<ViewActivityHistoryResponse>();

                        while (await reader.ReadAsync())
                        {
                            List<SnapDetails> snaps = new();

                            var snapsJsonObj = reader["SnapsJson"];

                            if (snapsJsonObj != DBNull.Value)
                            {
                                var snapsJson = snapsJsonObj?.ToString();

                                if (!string.IsNullOrWhiteSpace(snapsJson))
                                {
                                    snaps = JsonSerializer.Deserialize<List<SnapDetails>>(snapsJson)
                                            ?? new List<SnapDetails>();
                                }
                            }

                            DateTime? startOn =
                                reader["StartOn"] == DBNull.Value
                                    ? null
                                    : Convert.ToDateTime(reader["StartOn"]);

                            DateTime? endOn =
                                reader["EndOn"] == DBNull.Value
                                    ? null
                                    : Convert.ToDateTime(reader["EndOn"]);

                            list.Add(new ViewActivityHistoryResponse
                            {
                                UserTrackingId =
                                    reader["UserTrackingId"] == DBNull.Value
                                        ? 0
                                        : Convert.ToInt32(reader["UserTrackingId"]),

                                UserId =
                                    reader["UserId"] == DBNull.Value
                                        ? 0
                                        : Convert.ToInt32(reader["UserId"]),

                                MachineName =
                                    reader["MachineName"] == DBNull.Value
                                        ? string.Empty
                                        : reader["MachineName"].ToString() ?? string.Empty,

                                StartOn =
                                    startOn?.ToString("yyyy-MM-dd") ?? string.Empty,

                                EndOn =
                                    endOn?.ToString("yyyy-MM-dd") ?? string.Empty,

                                TimeRange = FormatTimeRange(startOn, endOn),

                                Snaps = snaps
                            });
                        }

                        return list;
                    })
                    ?? new List<ViewActivityHistoryResponse>();
            }
            catch (Exception ex)
            {
                _logHelper.LogError(
                    nameof(GetActivityHistoryByUserAsync),
                    ex);

                return new List<ViewActivityHistoryResponse>();
            }
        }

        private static string FormatTimeRange(DateTime? start, DateTime? end)
        {
            if (!start.HasValue || !end.HasValue)
                return string.Empty;

            var duration = end.Value - start.Value;

            int hours = (int)duration.TotalHours;
            int mins = duration.Minutes;

            return $"{start.Value:h:mm tt} - {end.Value:h:mm tt} ({hours}:{mins:D2} hrs)";
        }
    }
}