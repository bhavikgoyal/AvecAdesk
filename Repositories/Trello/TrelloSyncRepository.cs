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
