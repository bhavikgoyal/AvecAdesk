using AvecADeskApi.DTOs.Trello;
using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using Microsoft.Data.SqlClient;

namespace AvecADeskApi.Repositories.Trello
{
    public class TrelloSyncRepository : ITrelloSyncRepository
    {
        private readonly SqlDbHelper _db;
        private readonly LogHelper _logHelper;

        public TrelloSyncRepository(SqlDbHelper db, LogHelper logHelper)
        {
            _db = db;
            _logHelper = logHelper;
        }

        public async Task<List<TrelloBoardLinkResponse>> GetBoardLinksAsync()
        {
            try
            {
                return await _db.ExecuteReaderListAsync("dbo.SP_TrelloBoardLink_GetAll", _ => { }, MapLink);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(GetBoardLinksAsync)}", ex);
                throw;
            }
        }

        public async Task<TrelloBoardLinkResponse?> GetBoardLinkAsync(int localBoardId)
        {
            try
            {
                return await _db.ExecuteReaderSingleAsync(
                    "dbo.SP_TrelloBoardLink_Get",
                    cmd => cmd.Parameters.AddWithValue("@LocalBoardID", localBoardId),
                    MapLink);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(GetBoardLinkAsync)}", ex);
                throw;
            }
        }

        public async Task<TrelloBoardLinkResponse?> SaveBoardLinkAsync(int localBoardId, string trelloBoardId, string? trelloBoardName, string initialWinner, int userId)
        {
            try
            {
                return await _db.ExecuteReaderSingleAsync(
                    "dbo.SP_TrelloBoardLink_Save",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@LocalBoardID", localBoardId);
                        cmd.Parameters.AddWithValue("@TrelloBoardID", trelloBoardId);
                        cmd.Parameters.AddWithValue("@TrelloBoardName", (object?)trelloBoardName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@InitialWinner", initialWinner);
                        cmd.Parameters.AddWithValue("@UserID", userId);
                    },
                    MapLink);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(SaveBoardLinkAsync)}", ex);
                throw;
            }
        }

        public async Task<bool> DeleteBoardLinkAsync(int localBoardId)
        {
            try
            {
                var rows = await _db.ExecuteScalarAsync(
                    "dbo.SP_TrelloBoardLink_Delete",
                    cmd => cmd.Parameters.AddWithValue("@LocalBoardID", localBoardId));
                return Convert.ToInt32(rows ?? 0) > 0;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(DeleteBoardLinkAsync)}", ex);
                throw;
            }
        }

        public async Task SetStatusAsync(int localBoardId, string? status, bool activate)
        {
            try
            {
                await _db.ExecuteNonQueryAsync("dbo.SP_TrelloBoardLink_SetStatus", cmd =>
                {
                    cmd.Parameters.AddWithValue("@LocalBoardID", localBoardId);
                    cmd.Parameters.AddWithValue("@Status", (object?)status ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Activate", activate);
                });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(SetStatusAsync)}", ex);
                throw;
            }
        }

        public async Task<List<TrelloMapRow>> GetMapsForBoardAsync(int localBoardId)
        {
            try
            {
                return await _db.ExecuteReaderListAsync(
                    "dbo.SP_TrelloSyncMap_GetForBoard",
                    cmd => cmd.Parameters.AddWithValue("@LocalBoardID", localBoardId),
                    reader => new TrelloMapRow
                    {
                        EntityType = reader["EntityType"].ToString() ?? string.Empty,
                        LocalID = Convert.ToInt32(reader["LocalID"]),
                        TrelloID = reader["TrelloID"].ToString() ?? string.Empty,
                        SyncHash = reader["SyncHash"] is DBNull ? null : reader["SyncHash"].ToString(),
                    });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(GetMapsForBoardAsync)}", ex);
                throw;
            }
        }

        public async Task SaveMapAsync(string entityType, int localId, string trelloId, int? localBoardId, string? syncHash)
        {
            try
            {
                await _db.ExecuteNonQueryAsync("dbo.SP_TrelloSyncMap_Save", cmd =>
                {
                    cmd.Parameters.AddWithValue("@EntityType", entityType);
                    cmd.Parameters.AddWithValue("@LocalID", localId);
                    cmd.Parameters.AddWithValue("@TrelloID", trelloId);
                    cmd.Parameters.AddWithValue("@LocalBoardID", (object?)localBoardId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SyncHash", (object?)syncHash ?? DBNull.Value);
                });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(SaveMapAsync)}", ex);
                throw;
            }
        }

        public async Task<List<LocalBoardRow>> GetLocalBoardsAsync()
        {
            try
            {
                return await _db.ExecuteReaderListAsync(
                    "dbo.SP_TrelloSync_GetLocalBoards",
                    _ => { },
                    reader => new LocalBoardRow
                    {
                        BoardID = Convert.ToInt32(reader["BoardID"]),
                        BoardName = reader["BoardName"] is DBNull ? string.Empty : reader["BoardName"].ToString() ?? string.Empty,
                        TrelloBoardID = reader["TrelloBoardID"] is DBNull ? null : reader["TrelloBoardID"].ToString(),
                    });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(GetLocalBoardsAsync)}", ex);
                throw;
            }
        }

        public async Task<int> CleanupOrphanLinksAsync()
        {
            try
            {
                var rows = await _db.ExecuteScalarAsync("dbo.SP_TrelloSync_CleanupOrphanLinks", _ => { });
                return Convert.ToInt32(rows ?? 0);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(CleanupOrphanLinksAsync)}", ex);
                throw;
            }
        }

        public async Task<List<(int LocalBoardID, string TrelloListID)>> GetBoardListMapsAsync()
        {
            try
            {
                return await _db.ExecuteReaderListAsync(
                    "dbo.SP_TrelloSync_GetBoardListMaps",
                    _ => { },
                    reader => (Convert.ToInt32(reader["LocalBoardID"]), reader["TrelloID"].ToString() ?? string.Empty));
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(GetBoardListMapsAsync)}", ex);
                throw;
            }
        }

        public async Task<int?> GetCardBoardIdAsync(int cardId)
        {
            try
            {
                var value = await _db.ExecuteScalarAsync(
                    "dbo.SP_TrelloSync_GetCardBoardID",
                    cmd => cmd.Parameters.AddWithValue("@CardID", cardId));
                return value == null || value is DBNull ? null : Convert.ToInt32(value);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(GetCardBoardIdAsync)}", ex);
                throw;
            }
        }

        public async Task<int?> GetOwnerUserIdByRoleAsync(int roleId)
        {
            try
            {
                var value = await _db.ExecuteScalarAsync(
                    "dbo.SP_TrelloSync_GetOwnerUserId",
                    cmd => cmd.Parameters.AddWithValue("@RoleId", roleId));
                return value == null || value is DBNull ? null : Convert.ToInt32(value);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(GetOwnerUserIdByRoleAsync)}", ex);
                throw;
            }
        }

        public async Task<List<StoredWebhookRow>> GetWebhooksAsync()
        {
            try
            {
                return await _db.ExecuteReaderListAsync(
                    "dbo.SP_TrelloWebhook_GetAll",
                    _ => { },
                    reader => new StoredWebhookRow
                    {
                        WebhookID = reader["WebhookID"].ToString() ?? string.Empty,
                        IdModel = reader["IdModel"].ToString() ?? string.Empty,
                        CallbackUrl = reader["CallbackUrl"].ToString() ?? string.Empty,
                    });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(GetWebhooksAsync)}", ex);
                throw;
            }
        }

        public async Task SaveWebhookAsync(string webhookId, string idModel, string callbackUrl)
        {
            try
            {
                await _db.ExecuteNonQueryAsync("dbo.SP_TrelloWebhook_Save", cmd =>
                {
                    cmd.Parameters.AddWithValue("@WebhookID", webhookId);
                    cmd.Parameters.AddWithValue("@IdModel", idModel);
                    cmd.Parameters.AddWithValue("@CallbackUrl", callbackUrl);
                });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(SaveWebhookAsync)}", ex);
                throw;
            }
        }

        public async Task DeleteWebhookAsync(string webhookId)
        {
            try
            {
                await _db.ExecuteNonQueryAsync("dbo.SP_TrelloWebhook_Delete",
                    cmd => cmd.Parameters.AddWithValue("@WebhookID", webhookId));
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(DeleteWebhookAsync)}", ex);
                throw;
            }
        }

        public async Task<List<LocalChecklistRow>> GetBoardChecklistsAsync(int localBoardId)
        {
            try
            {
                var result = await _db.ExecuteReaderCustomAsync(
                    "dbo.SP_TrelloSync_GetBoardChecklists",
                    cmd => cmd.Parameters.AddWithValue("@LocalBoardID", localBoardId),
                    async reader =>
                    {
                        var checklists = new Dictionary<int, LocalChecklistRow>();
                        while (await reader.ReadAsync())
                        {
                            var checklistId = Convert.ToInt32(reader["ChecklistID"]);
                            if (!checklists.TryGetValue(checklistId, out var checklist))
                            {
                                checklist = new LocalChecklistRow
                                {
                                    ChecklistID = checklistId,
                                    CardID = Convert.ToInt32(reader["CardID"]),
                                    ChecklistTitle = reader["ChecklistTitle"] as string ?? string.Empty,
                                    TrelloChecklistID = NullIfBlank(reader["TrelloChecklistsId"]),
                                };
                                checklists[checklistId] = checklist;
                            }

                            if (reader["ChecklistItemID"] is DBNull) continue;
                            checklist.Items.Add(new LocalCheckItemRow
                            {
                                ChecklistItemID = Convert.ToInt32(reader["ChecklistItemID"]),
                                ItemName = reader["ItemName"] as string ?? string.Empty,
                                IsCompleted = reader["IsCompleted"] is not DBNull && Convert.ToBoolean(reader["IsCompleted"]),
                                Position = reader["Position"] is DBNull ? null : Convert.ToInt32(reader["Position"]),
                                TrelloItemID = NullIfBlank(reader["TrelloChecklistItemsId"]),
                                SyncHash = NullIfBlank(reader["TrelloSyncHash"]),
                            });
                        }
                        return checklists.Values.ToList();
                    });
                return result ?? new List<LocalChecklistRow>();
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(GetBoardChecklistsAsync)}", ex);
                throw;
            }
        }

        public async Task<int> SaveChecklistAsync(int? checklistId, int cardId, string title, string trelloChecklistId, DateTime? createdAt)
        {
            try
            {
                var value = await _db.ExecuteScalarAsync("dbo.SP_TrelloSync_SaveChecklist", cmd =>
                {
                    cmd.Parameters.AddWithValue("@ChecklistID", (object?)checklistId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CardID", cardId);
                    cmd.Parameters.AddWithValue("@ChecklistTitle", title);
                    cmd.Parameters.AddWithValue("@TrelloChecklistID", trelloChecklistId);
                    cmd.Parameters.AddWithValue("@CreatedAt", (object?)createdAt ?? DBNull.Value);
                });
                return Convert.ToInt32(value);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(SaveChecklistAsync)}", ex);
                throw;
            }
        }

        public async Task<int> SaveChecklistItemAsync(int? checklistItemId, int checklistId, string itemName, bool isCompleted, int? position,
            string trelloItemId, string? syncHash, DateTime? createdAt)
        {
            try
            {
                var value = await _db.ExecuteScalarAsync("dbo.SP_TrelloSync_SaveChecklistItem", cmd =>
                {
                    cmd.Parameters.AddWithValue("@ChecklistItemID", (object?)checklistItemId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ChecklistID", checklistId);
                    cmd.Parameters.AddWithValue("@ItemName", itemName);
                    cmd.Parameters.AddWithValue("@IsCompleted", isCompleted);
                    cmd.Parameters.AddWithValue("@Position", (object?)position ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@TrelloItemID", trelloItemId);
                    cmd.Parameters.AddWithValue("@SyncHash", (object?)syncHash ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CreatedAt", (object?)createdAt ?? DBNull.Value);
                });
                return Convert.ToInt32(value);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(SaveChecklistItemAsync)}", ex);
                throw;
            }
        }

        public async Task<List<LocalCommentRow>> GetBoardCommentsAsync(int localBoardId)
        {
            try
            {
                return await _db.ExecuteReaderListAsync(
                    "dbo.SP_TrelloSync_GetBoardComments",
                    cmd => cmd.Parameters.AddWithValue("@LocalBoardID", localBoardId),
                    reader =>
                    {
                        var fullName = $"{reader["FirstName"] as string} {reader["LastName"] as string}".Trim();
                        return new LocalCommentRow
                        {
                            CommentID = Convert.ToInt32(reader["CommentID"]),
                            CardID = Convert.ToInt32(reader["CardID"]),
                            UserID = Convert.ToInt32(reader["UserID"]),
                            AuthorName = fullName.Length > 0 ? fullName : reader["UserName"] as string ?? "AiDesk user",
                            AuthorTrelloId = NullIfBlank(reader["TrelloUserId"])?.Trim(),
                            CommentText = reader["CommentText"] as string ?? string.Empty,
                            TrelloCommentID = NullIfBlank(reader["TrellocCardCommentsId"]),
                            SyncHash = NullIfBlank(reader["TrelloSyncHash"]),
                        };
                    });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(GetBoardCommentsAsync)}", ex);
                throw;
            }
        }

        public async Task<int> SaveCommentAsync(int? commentId, int cardId, int userId, string commentText, DateTime? createdAtUtc,
            string trelloCommentId, string? syncHash)
        {
            try
            {
                var value = await _db.ExecuteScalarAsync("dbo.SP_TrelloSync_SaveComment", cmd =>
                {
                    cmd.Parameters.AddWithValue("@CommentID", (object?)commentId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CardID", cardId);
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@CommentText", commentText);
                    cmd.Parameters.AddWithValue("@CreatedAt", (object?)createdAtUtc ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@TrelloCommentID", trelloCommentId);
                    cmd.Parameters.AddWithValue("@SyncHash", (object?)syncHash ?? DBNull.Value);
                });
                return Convert.ToInt32(value);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(SaveCommentAsync)}", ex);
                throw;
            }
        }

        public async Task<Dictionary<string, int>> GetTrelloUserMapAsync()
        {
            try
            {
                var rows = await _db.ExecuteReaderListAsync(
                    "dbo.SP_TrelloSync_GetTrelloUsers",
                    _ => { },
                    reader => (TrelloUserId: reader["TrelloUserId"].ToString() ?? string.Empty, UserId: Convert.ToInt32(reader["UserId"])));

                var map = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var row in rows) map.TryAdd(row.TrelloUserId, row.UserId);
                return map;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(GetTrelloUserMapAsync)}", ex);
                throw;
            }
        }

        public async Task<TrelloLocalRef?> GetLocalRefAsync(string entityType, int localId)
        {
            try
            {
                return await _db.ExecuteReaderSingleAsync(
                    "dbo.SP_TrelloSync_GetLocalRef",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@EntityType", entityType);
                        cmd.Parameters.AddWithValue("@LocalID", localId);
                    },
                    reader => new TrelloLocalRef
                    {
                        TrelloID = NullIfBlank(reader["TrelloID"]),
                        TrelloParentID = NullIfBlank(reader["TrelloParentID"]),
                        CardID = Convert.ToInt32(reader["CardID"]),
                        BoardID = reader["BoardID"] is DBNull ? null : Convert.ToInt32(reader["BoardID"]),
                    });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(GetLocalRefAsync)}", ex);
                throw;
            }
        }

        public async Task DeleteLocalAsync(string entityType, int localId)
        {
            try
            {
                await _db.ExecuteNonQueryAsync("dbo.SP_TrelloSync_DeleteLocal", cmd =>
                {
                    cmd.Parameters.AddWithValue("@EntityType", entityType);
                    cmd.Parameters.AddWithValue("@LocalID", localId);
                });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(DeleteLocalAsync)}", ex);
                throw;
            }
        }

        public async Task AddTombstoneAsync(string entityType, string trelloId, string? trelloParentId, int? localBoardId)
        {
            try
            {
                await _db.ExecuteNonQueryAsync("dbo.SP_TrelloSync_AddTombstone", cmd =>
                {
                    cmd.Parameters.AddWithValue("@EntityType", entityType);
                    cmd.Parameters.AddWithValue("@TrelloID", trelloId);
                    cmd.Parameters.AddWithValue("@TrelloParentID", (object?)trelloParentId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LocalBoardID", (object?)localBoardId ?? DBNull.Value);
                });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(AddTombstoneAsync)}", ex);
                throw;
            }
        }

        public async Task<List<TrelloTombstoneRow>> GetTombstonesAsync(int localBoardId)
        {
            try
            {
                return await _db.ExecuteReaderListAsync(
                    "dbo.SP_TrelloSync_GetTombstones",
                    cmd => cmd.Parameters.AddWithValue("@LocalBoardID", localBoardId),
                    reader => new TrelloTombstoneRow
                    {
                        TrelloID = reader["TrelloID"].ToString() ?? string.Empty,
                        EntityType = reader["EntityType"].ToString() ?? string.Empty,
                        TrelloParentID = NullIfBlank(reader["TrelloParentID"]),
                        ProcessedAt = reader["ProcessedAt"] is DBNull ? null : Convert.ToDateTime(reader["ProcessedAt"]),
                    });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(GetTombstonesAsync)}", ex);
                throw;
            }
        }

        public async Task MarkTombstoneAsync(string trelloId, string? error)
        {
            try
            {
                await _db.ExecuteNonQueryAsync("dbo.SP_TrelloSync_MarkTombstone", cmd =>
                {
                    cmd.Parameters.AddWithValue("@TrelloID", trelloId);
                    cmd.Parameters.AddWithValue("@Error", (object?)error ?? DBNull.Value);
                });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(MarkTombstoneAsync)}", ex);
                throw;
            }
        }

        public async Task<string?> GetLastActivityIdAsync(int localBoardId)
        {
            try
            {
                var value = await _db.ExecuteScalarAsync("dbo.SP_TrelloSync_GetLastActivityId",
                    cmd => cmd.Parameters.AddWithValue("@LocalBoardID", localBoardId));
                return value == null ? null : NullIfBlank(value);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(GetLastActivityIdAsync)}", ex);
                throw;
            }
        }

        public async Task<bool> SaveActivityAsync(TrelloActivityImport activity)
        {
            try
            {
                var value = await _db.ExecuteScalarAsync("dbo.SP_TrelloSync_SaveActivity", cmd =>
                {
                    cmd.Parameters.AddWithValue("@CardID", activity.CardID);
                    cmd.Parameters.AddWithValue("@UserID", activity.UserID);
                    cmd.Parameters.AddWithValue("@TrelloMemberName", (object?)activity.TrelloMemberName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ActivityType", activity.ActivityType);
                    cmd.Parameters.AddWithValue("@ActivityDescription", (object?)activity.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@OldValue", (object?)activity.OldValue ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@NewValue", (object?)activity.NewValue ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CreatedAt", activity.CreatedAtUtc);
                    cmd.Parameters.AddWithValue("@TrelloActivityId", activity.TrelloActivityId);
                    cmd.Parameters.AddWithValue("@EchoOfAiDesk", activity.EchoOfAiDesk);
                    cmd.Parameters.AddWithValue("@EchoDescription", (object?)activity.EchoDescription ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EchoNewValue", (object?)activity.EchoNewValue ?? DBNull.Value);
                });
                return Convert.ToInt32(value ?? 0) > 0;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(SaveActivityAsync)}", ex);
                throw;
            }
        }

        public async Task<List<LocalBoardLabelRow>> GetBoardLabelsAsync(int localBoardId)
        {
            try
            {
                return await _db.ExecuteReaderListAsync(
                    "dbo.SP_TrelloSync_GetBoardLabels",
                    cmd => cmd.Parameters.AddWithValue("@LocalBoardID", localBoardId),
                    reader => new LocalBoardLabelRow
                    {
                        BoardLabelID = Convert.ToInt32(reader["BoardLabelID"]),
                        LabelName = reader["LabelName"] as string ?? string.Empty,
                        Color = reader["Color"] as string ?? string.Empty,
                    });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(GetBoardLabelsAsync)}", ex);
                throw;
            }
        }

        public async Task<int> SaveBoardLabelAsync(int? boardLabelId, int localBoardId, string labelName, string color)
        {
            try
            {
                var value = await _db.ExecuteScalarAsync("dbo.SP_TrelloSync_SaveBoardLabel", cmd =>
                {
                    cmd.Parameters.AddWithValue("@BoardLabelID", (object?)boardLabelId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LocalBoardID", localBoardId);
                    cmd.Parameters.AddWithValue("@LabelName", labelName);
                    cmd.Parameters.AddWithValue("@Color", color);
                });
                return Convert.ToInt32(value);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(SaveBoardLabelAsync)}", ex);
                throw;
            }
        }

        public async Task DeleteBoardLabelAsync(int boardLabelId)
        {
            try
            {
                await _db.ExecuteScalarAsync("dbo.SP_DeleteBoardLabel",
                    cmd => cmd.Parameters.AddWithValue("@BoardLabelID", boardLabelId));
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(DeleteBoardLabelAsync)}", ex);
                throw;
            }
        }

        public async Task<List<(int CardID, int BoardLabelID)>> GetBoardCardLabelsAsync(int localBoardId)
        {
            try
            {
                return await _db.ExecuteReaderListAsync(
                    "dbo.SP_TrelloSync_GetBoardCardLabels",
                    cmd => cmd.Parameters.AddWithValue("@LocalBoardID", localBoardId),
                    reader => (Convert.ToInt32(reader["CardID"]), Convert.ToInt32(reader["BoardLabelID"])));
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(GetBoardCardLabelsAsync)}", ex);
                throw;
            }
        }

        public async Task<List<int>> SetCardLabelsAsync(int cardId, IEnumerable<int> boardLabelIds)
        {
            try
            {
                return await _db.ExecuteReaderListAsync(
                    "dbo.SP_TrelloSync_SetCardLabels",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@CardID", cardId);
                        cmd.Parameters.AddWithValue("@BoardLabelIDs", string.Join(",", boardLabelIds));
                    },
                    reader => Convert.ToInt32(reader["BoardLabelID"]));
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(SetCardLabelsAsync)}", ex);
                throw;
            }
        }

        public async Task<List<LocalCardDetailsRow>> GetBoardCardDetailsAsync(int localBoardId)
        {
            try
            {
                return await _db.ExecuteReaderListAsync(
                    "dbo.SP_TrelloSync_GetBoardCardDetails",
                    cmd => cmd.Parameters.AddWithValue("@LocalBoardID", localBoardId),
                    reader => new LocalCardDetailsRow
                    {
                        CardID = Convert.ToInt32(reader["CardID"]),
                        StartDate = reader["StartDate"] is DBNull ? null : Convert.ToDateTime(reader["StartDate"]),
                        CoverColor = NullIfBlank(reader["CoverColor"]),
                        CoverImageUrl = NullIfBlank(reader["CoverImageUrl"]),
                        CoverSize = NullIfBlank(reader["CoverSize"]),
                        CoverBrightness = NullIfBlank(reader["CoverBrightness"]),
                    });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(GetBoardCardDetailsAsync)}", ex);
                throw;
            }
        }

        public async Task SetCardStartDateAsync(int cardId, DateTime? startDate)
        {
            try
            {
                await _db.ExecuteNonQueryAsync("dbo.SP_TrelloSync_SetCardStartDate", cmd =>
                {
                    cmd.Parameters.AddWithValue("@CardID", cardId);
                    cmd.Parameters.AddWithValue("@StartDate", (object?)startDate ?? DBNull.Value);
                });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(SetCardStartDateAsync)}", ex);
                throw;
            }
        }

        public async Task SaveCardCoverAsync(int cardId, string color, string size, string brightness)
        {
            try
            {
                await _db.ExecuteNonQueryAsync("dbo.SP_UpsertCardCover", cmd =>
                {
                    cmd.Parameters.AddWithValue("@CardID", cardId);
                    cmd.Parameters.AddWithValue("@CoverColor", color);
                    cmd.Parameters.AddWithValue("@CoverImageUrl", DBNull.Value);
                    cmd.Parameters.AddWithValue("@CoverSize", size);
                    cmd.Parameters.AddWithValue("@CoverBrightness", brightness);
                    cmd.Parameters.AddWithValue("@UserID", DBNull.Value);
                });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(SaveCardCoverAsync)}", ex);
                throw;
            }
        }

        public async Task RemoveCardCoverAsync(int cardId)
        {
            try
            {
                await _db.ExecuteScalarAsync("dbo.SP_RemoveCardCover",
                    cmd => cmd.Parameters.AddWithValue("@CardID", cardId));
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(TrelloSyncRepository)}.{nameof(RemoveCardCoverAsync)}", ex);
                throw;
            }
        }

        private static string? NullIfBlank(object value) =>
            value is DBNull || string.IsNullOrWhiteSpace(value?.ToString()) ? null : value.ToString();

        private static TrelloBoardLinkResponse MapLink(SqlDataReader reader) => new()
        {
            LocalBoardID = Convert.ToInt32(reader["LocalBoardID"]),
            LocalBoardName = reader["LocalBoardName"] is DBNull ? null : reader["LocalBoardName"].ToString(),
            TrelloBoardID = reader["TrelloBoardID"].ToString() ?? string.Empty,
            TrelloBoardName = reader["TrelloBoardName"] is DBNull ? null : reader["TrelloBoardName"].ToString(),
            InitialWinner = reader["InitialWinner"].ToString() ?? "trello",
            IsActive = Convert.ToBoolean(reader["IsActive"]),
            CreatedBy = Convert.ToInt32(reader["CreatedBy"]),
            CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
            LastSyncedAt = reader["LastSyncedAt"] is DBNull ? null : Convert.ToDateTime(reader["LastSyncedAt"]),
            LastSyncStatus = reader["LastSyncStatus"] is DBNull ? null : reader["LastSyncStatus"].ToString(),
        };
    }
}
