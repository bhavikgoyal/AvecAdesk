using AvecADeskApi.DTOs;
using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Cardlist;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AvecADeskApi.Repositories.Checklist
{
    public class ChecklistRepository : IChecklistRepository
    {
        private readonly SqlDbHelper _db;
        private readonly LogHelper _logHelper;

        public ChecklistRepository(SqlDbHelper db, LogHelper logHelper)
        {
            _db = db;
            _logHelper = logHelper;
        }

        public async Task<List<ChecklistModel>> GetByCardIdAsync(int cardId)
        {
            try
            {
                var result = await _db.ExecuteReaderCustomAsync(
                    "dbo.SP_GetChecklistsByCardId",
                    cmd => cmd.Parameters.AddWithValue("@CardID", cardId),
                    async reader =>
                    {
                        var checklists = new Dictionary<int, ChecklistModel>();

                        while (await reader.ReadAsync())
                        {
                            var clId = reader.GetInt32(reader.GetOrdinal("ChecklistID"));

                            if (!checklists.ContainsKey(clId))
                            {
                                checklists[clId] = new ChecklistModel
                                {
                                    ChecklistID = clId,
                                    CardID = reader.GetInt32(reader.GetOrdinal("CardID")),
                                    ChecklistTitle = reader["ChecklistTitle"] as string ?? "",
                                    CreatedAt = reader["CreatedAt"] is DBNull ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                                    Items = new List<ChecklistItemModel>()
                                };
                            }

                            if (reader["ChecklistItemID"] is not DBNull)
                            {
                                checklists[clId].Items.Add(new ChecklistItemModel
                                {
                                    ChecklistItemID = reader.GetInt32(reader.GetOrdinal("ChecklistItemID")),
                                    ChecklistID = clId,
                                    ItemName = reader["ItemName"] as string ?? "",
                                    IsCompleted = reader["IsCompleted"] is not DBNull && reader.GetBoolean(reader.GetOrdinal("IsCompleted")),
                                    Position = reader["Position"] is DBNull ? null : (int?)reader.GetInt32(reader.GetOrdinal("Position")),
                                    CreatedAt = reader["ItemCreatedAt"] is DBNull ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("ItemCreatedAt")),
                                });
                            }
                        }

                        return checklists.Values.ToList() as List<ChecklistModel>;
                    });

                return result ?? new List<ChecklistModel>();
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(ChecklistRepository)}.{nameof(GetByCardIdAsync)}", ex);
                throw;
            }
        }

        public async Task<int> CreateChecklistAsync(CreateChecklistRequest request)
        {
            try
            {
                var newIdParam = new SqlParameter("@NewChecklistId", SqlDbType.Int) { Direction = ParameterDirection.Output };

                await _db.ExecuteNonQueryAsync("dbo.SP_InsertChecklist", cmd =>
                {
                    cmd.Parameters.AddWithValue("@CardID", request.CardID);
                    cmd.Parameters.AddWithValue("@ChecklistTitle", request.ChecklistTitle);
                    cmd.Parameters.Add(newIdParam);
                });

                return (int)newIdParam.Value;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(ChecklistRepository)}.{nameof(CreateChecklistAsync)}", ex);
                throw;
            }
        }

        public async Task<bool> DeleteChecklistAsync(int checklistId)
        {
            try
            {
                var rows = await _db.ExecuteNonQueryWithResultAsync("dbo.SP_DeleteChecklist", cmd =>
                {
                    cmd.Parameters.AddWithValue("@ChecklistID", checklistId);
                });

                return rows > 0;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(ChecklistRepository)}.{nameof(DeleteChecklistAsync)}", ex);
                throw;
            }
        }

        public async Task<int> CreateChecklistItemAsync(CreateChecklistItemRequest request)
        {
            try
            {
                var newIdParam = new SqlParameter("@NewItemId", SqlDbType.Int) { Direction = ParameterDirection.Output };

                await _db.ExecuteNonQueryAsync("dbo.SP_InsertChecklistItem", cmd =>
                {
                    cmd.Parameters.AddWithValue("@ChecklistID", request.ChecklistID);
                    cmd.Parameters.AddWithValue("@ItemName", request.ItemName);
                    cmd.Parameters.Add(newIdParam);
                });

                return (int)newIdParam.Value;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(ChecklistRepository)}.{nameof(CreateChecklistItemAsync)}", ex);
                throw;
            }
        }

        public async Task<bool> UpdateChecklistItemStatusAsync(int checklistItemId, bool isCompleted)
        {
            try
            {
                var rows = await _db.ExecuteNonQueryWithResultAsync("dbo.SP_UpdateChecklistItemStatus_New", cmd =>
                {
                    cmd.Parameters.AddWithValue("@ChecklistItemID", checklistItemId);
                    cmd.Parameters.AddWithValue("@IsCompleted", isCompleted);
                });

                return rows > 0;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(ChecklistRepository)}.{nameof(UpdateChecklistItemStatusAsync)}", ex);
                throw;
            }
        }

        public async Task<bool> DeleteChecklistItemAsync(int checklistItemId)
        {
            try
            {
                var rows = await _db.ExecuteNonQueryWithResultAsync("dbo.SP_DeleteChecklistItem", cmd =>
                {
                    cmd.Parameters.AddWithValue("@ChecklistItemID", checklistItemId);
                });

                return rows > 0;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(ChecklistRepository)}.{nameof(DeleteChecklistItemAsync)}", ex);
                throw;
            }
        }
        public async Task<bool> UpdateChecklistItemNameAsync(int checklistItemId, string itemName)
        {
            try
            {
                var rows = await _db.ExecuteNonQueryWithResultAsync("dbo.SP_UpdateChecklistItemName", cmd =>
                {
                    cmd.Parameters.AddWithValue("@ChecklistItemID", checklistItemId);
                    cmd.Parameters.AddWithValue("@ItemName", itemName);
                });

                return rows > 0;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(ChecklistRepository)}.{nameof(UpdateChecklistItemNameAsync)}", ex);
                throw;
            }
        }
    }
}
