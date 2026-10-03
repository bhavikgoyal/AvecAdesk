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

        public async Task<List<BoardLabelResponse>> GetBoardLabelsForCardAsync(int cardId)
        {
            try
            {
                return await _db.ExecuteReaderListAsync(
                    "dbo.SP_GetBoardLabelsForCard",
                    cmd => cmd.Parameters.AddWithValue("@CardID", cardId),
                    MapBoardLabel);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(LabelRepository)}.{nameof(GetBoardLabelsForCardAsync)}", ex);
                throw;
            }
        }

        public async Task<BoardLabelResponse?> CreateBoardLabelAsync(CreateBoardLabelRequest request, int? userId)
        {
            try
            {
                return await _db.ExecuteReaderSingleAsync(
                    "dbo.SP_CreateBoardLabel",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@CardID", request.CardID);
                        cmd.Parameters.AddWithValue("@LabelName", request.LabelName ?? string.Empty);
                        cmd.Parameters.AddWithValue("@Color", request.Color ?? string.Empty);
                        cmd.Parameters.AddWithValue("@UserID", (object?)userId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@AssignToCard", request.AssignToCard);
                    },
                    MapBoardLabel);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(LabelRepository)}.{nameof(CreateBoardLabelAsync)}", ex);
                throw;
            }
        }

        public async Task<BoardLabelResponse?> UpdateBoardLabelAsync(int boardLabelId, UpdateBoardLabelRequest request)
        {
            try
            {
                return await _db.ExecuteReaderSingleAsync(
                    "dbo.SP_UpdateBoardLabel",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@BoardLabelID", boardLabelId);
                        cmd.Parameters.AddWithValue("@LabelName", request.LabelName ?? string.Empty);
                        cmd.Parameters.AddWithValue("@Color", request.Color ?? string.Empty);
                    },
                    MapBoardLabel);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(LabelRepository)}.{nameof(UpdateBoardLabelAsync)}", ex);
                throw;
            }
        }

        public async Task<bool> DeleteBoardLabelAsync(int boardLabelId)
        {
            try
            {
                var result = await _db.ExecuteScalarAsync(
                    "dbo.SP_DeleteBoardLabel",
                    cmd => cmd.Parameters.AddWithValue("@BoardLabelID", boardLabelId));

                return result != null && result != DBNull.Value && Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(LabelRepository)}.{nameof(DeleteBoardLabelAsync)}", ex);
                throw;
            }
        }

        public async Task<BoardLabelResponse?> SetCardBoardLabelAsync(SetCardBoardLabelRequest request)
        {
            try
            {
                return await _db.ExecuteReaderSingleAsync(
                    "dbo.SP_SetCardBoardLabel",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@CardID", request.CardID);
                        cmd.Parameters.AddWithValue("@BoardLabelID", request.BoardLabelID);
                        cmd.Parameters.AddWithValue("@IsAssigned", request.IsAssigned);
                    },
                    MapBoardLabel);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(LabelRepository)}.{nameof(SetCardBoardLabelAsync)}", ex);
                throw;
            }
        }

        private static BoardLabelResponse MapBoardLabel(SqlDataReader reader)
        {
            var hasAssigned = HasColumn(reader, "IsAssigned");
            var hasCardLabel = HasColumn(reader, "CardLabelID");
            return new BoardLabelResponse
            {
                BoardLabelID = reader.GetInt32(reader.GetOrdinal("BoardLabelID")),
                BoardID = reader["BoardID"] is DBNull ? null : reader.GetInt32(reader.GetOrdinal("BoardID")),
                LabelName = reader["LabelName"] as string ?? string.Empty,
                Color = reader["Color"] as string ?? string.Empty,
                IsAssigned = hasAssigned && reader["IsAssigned"] is bool assigned && assigned,
                CardLabelID = hasCardLabel && reader["CardLabelID"] is not DBNull
                    ? reader.GetInt32(reader.GetOrdinal("CardLabelID"))
                    : null,
            };
        }

        private static bool HasColumn(SqlDataReader reader, string name)
        {
            for (var i = 0; i < reader.FieldCount; i++)
            {
                if (string.Equals(reader.GetName(i), name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
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