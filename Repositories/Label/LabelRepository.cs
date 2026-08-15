using AvecADeskApi.DTOs.Label;
using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using Microsoft.Data.SqlClient;

namespace AvecADeskApi.Repositories.Label
{
    public class LabelRepository : ILabelRepository
    {
        private readonly SqlDbHelper _db;
        private readonly LogHelper _logHelper;
        
        public LabelRepository(SqlDbHelper db, LogHelper logHelper)
        {
            _db = db;
            _logHelper = logHelper;
            
        }

        public async Task<List<LabelResponse>> GetByCardIdAsync(int cardId)
        {
            try
            {
                return await _db.ExecuteReaderListAsync(
                    "dbo.SP_GetLabelsByCardId",
                    cmd => cmd.Parameters.AddWithValue("@CardID", cardId),
                    MapLabel);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(LabelRepository)}.{nameof(GetByCardIdAsync)}", ex);
                throw;
            }
        }

        // NEW: batch fetch — board load ke liye N+1 se bachne ke liye
        public async Task<Dictionary<int, List<LabelResponse>>> GetByCardIdsAsync(IEnumerable<int> cardIds)
        {
            var idList = cardIds.Distinct().ToList();
            if (idList.Count == 0) return new Dictionary<int, List<LabelResponse>>();

            try
            {
                var csv = string.Join(",", idList);

                var flatLabels = await _db.ExecuteReaderListAsync(
                    "dbo.SP_GetLabelsByCardIds",
                    cmd => cmd.Parameters.AddWithValue("@CardIDs", csv),
                    MapLabel) ?? new List<LabelResponse>();   // NEW

                return flatLabels
                    .GroupBy(l => l.CardID)
                    .ToDictionary(g => g.Key, g => g.ToList());
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(LabelRepository)}.{nameof(GetByCardIdsAsync)}", ex);
                throw;
            }
        }
        public async Task<LabelResponse> CreateLabelAsync(CreateLabelRequest request)
        {
            try
            {
                var created = await _db.ExecuteReaderSingleAsync(
                    "dbo.SP_InsertLabel",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@CardID", request.CardID);
                        cmd.Parameters.AddWithValue("@LabelName", request.LabelName);
                        cmd.Parameters.AddWithValue("@Color", string.IsNullOrWhiteSpace(request.Color) ? string.Empty : request.Color.Trim());
                    },
                    MapLabel);

                if (created == null)
                    throw new InvalidOperationException("Failed to create label.");

                await SyncCardColorAsync(request.CardID); // NEW: card.Color ko sync karo

                return created;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(LabelRepository)}.{nameof(CreateLabelAsync)}", ex);
                throw;
            }
        }

        public async Task<bool> DeleteLabelAsync(int labelId)
        {
            try
            {
                await _db.ExecuteNonQueryAsync("dbo.SP_DeleteLabel", cmd =>
                {
                    cmd.Parameters.AddWithValue("@LabelID", labelId);
                });

                return true;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(LabelRepository)}.{nameof(DeleteLabelAsync)}", ex);
                throw;
            }
        }
        public async Task SyncCardColorAsync(int cardId)
        {
            try
            {
                await _db.ExecuteNonQueryAsync("dbo.SP_SyncCardColorFromLabels", cmd =>
                {
                    cmd.Parameters.AddWithValue("@CardID", cardId);
                });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(LabelRepository)}.{nameof(SyncCardColorAsync)}", ex);
                throw;
            }
        }

        private static LabelResponse MapLabel(SqlDataReader reader)
        {
            var color = reader["Color"] is DBNull ? null : reader["Color"] as string;
            return new LabelResponse
            {
                LabelID = reader.GetInt32(reader.GetOrdinal("LabelID")),
                CardID = reader.GetInt32(reader.GetOrdinal("CardID")),
                LabelName = reader["LabelName"] as string ?? string.Empty,
                Color = string.IsNullOrWhiteSpace(color) ? null : color,
            };
        }
    }
}