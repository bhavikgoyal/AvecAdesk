using AvecADeskApi.DTOs.Card;
using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using Microsoft.Data.SqlClient;

namespace AvecADeskApi.Repositories.CardAttachment
{
    public class CardAttachmentRepository : ICardAttachmentRepository
    {
        private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg" };

        private readonly SqlDbHelper _db;
        private readonly LogHelper _logHelper;

        public CardAttachmentRepository(SqlDbHelper db, LogHelper logHelper)
        {
            _db = db;
            _logHelper = logHelper;
        }

        public async Task<List<CardAttachmentResponse>> GetByCardIdAsync(int cardId)
        {
            try
            {
                return await _db.ExecuteReaderListAsync(
                    "dbo.SP_GetCardAttachmentsForCard",
                    cmd => cmd.Parameters.AddWithValue("@CardID", cardId),
                    MapAttachment);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardAttachmentRepository)}.{nameof(GetByCardIdAsync)}", ex);
                throw;
            }
        }

        public async Task<CardAttachmentResponse?> CreateAsync(NewCardAttachment attachment, int? userId)
        {
            try
            {
                return await _db.ExecuteReaderSingleAsync(
                    "dbo.SP_InsertCardAttachment",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@CardID", attachment.CardID);
                        cmd.Parameters.AddWithValue("@IsLink", attachment.IsLink);
                        cmd.Parameters.AddWithValue("@DisplayName", attachment.DisplayName);
                        cmd.Parameters.AddWithValue("@FileUrl", attachment.FileUrl);
                        cmd.Parameters.AddWithValue("@FileName", (object?)attachment.FileName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ContentType", (object?)attachment.ContentType ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@FileSize", (object?)attachment.FileSize ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@UserID", (object?)userId ?? DBNull.Value);
                    },
                    MapAttachment);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardAttachmentRepository)}.{nameof(CreateAsync)}", ex);
                throw;
            }
        }

        public async Task<CardAttachmentResponse?> UpdateAsync(int attachmentId, UpdateCardAttachmentRequest request)
        {
            try
            {
                return await _db.ExecuteReaderSingleAsync(
                    "dbo.SP_UpdateCardAttachment",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@AttachmentID", attachmentId);
                        cmd.Parameters.AddWithValue("@DisplayName", (object?)request.DisplayName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@LinkUrl", (object?)request.LinkUrl ?? DBNull.Value);
                    },
                    MapAttachment);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardAttachmentRepository)}.{nameof(UpdateAsync)}", ex);
                throw;
            }
        }

        public async Task<DeleteCardAttachmentResult> DeleteAsync(int attachmentId)
        {
            try
            {
                var result = await _db.ExecuteReaderSingleAsync(
                    "dbo.SP_DeleteCardAttachment",
                    cmd => cmd.Parameters.AddWithValue("@AttachmentID", attachmentId),
                    reader => new DeleteCardAttachmentResult
                    {
                        Deleted = Convert.ToInt32(reader["RowsAffected"]) > 0,
                        FileUrl = reader["FileUrl"] as string,
                        IsLink = reader["IsLink"] is bool isLink && isLink,
                    });

                return result ?? new DeleteCardAttachmentResult();
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardAttachmentRepository)}.{nameof(DeleteAsync)}", ex);
                throw;
            }
        }

        public async Task<Dictionary<int, int>> GetCountsByCardIdsAsync(IEnumerable<int> cardIds)
        {
            var idList = cardIds.Distinct().ToList();
            if (idList.Count == 0) return new Dictionary<int, int>();

            try
            {
                var rows = await _db.ExecuteReaderListAsync(
                    "dbo.SP_GetCardAttachmentCountsByCardIds",
                    cmd => cmd.Parameters.AddWithValue("@CardIDs", string.Join(",", idList)),
                    reader => new
                    {
                        CardID = reader.GetInt32(reader.GetOrdinal("CardID")),
                        Count = reader.GetInt32(reader.GetOrdinal("AttachmentCount")),
                    });

                return rows.ToDictionary(r => r.CardID, r => r.Count);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardAttachmentRepository)}.{nameof(GetCountsByCardIdsAsync)}", ex);
                throw;
            }
        }

        private static CardAttachmentResponse MapAttachment(SqlDataReader reader)
        {
            var isLink = reader["IsLink"] is bool link && link;
            var fileUrl = reader["FileUrl"] as string ?? string.Empty;
            var contentType = reader["ContentType"] as string;
            var createdOrdinal = reader.GetOrdinal("CreatedAt");

            return new CardAttachmentResponse
            {
                AttachmentID = reader.GetInt32(reader.GetOrdinal("AttachmentID")),
                CardID = reader.GetInt32(reader.GetOrdinal("CardID")),
                IsLink = isLink,
                DisplayName = reader["DisplayName"] as string ?? string.Empty,
                FileUrl = fileUrl,
                FileName = reader["FileName"] as string,
                ContentType = contentType,
                FileSize = reader["FileSize"] is DBNull ? null : Convert.ToInt64(reader["FileSize"]),
                UploadedBy = reader["UploadedBy"] is DBNull ? null : reader.GetInt32(reader.GetOrdinal("UploadedBy")),
                UploadedByName = BuildDisplayName(
                    reader["FirstName"] as string,
                    reader["LastName"] as string,
                    reader["UserName"] as string),
                CreatedAt = reader.IsDBNull(createdOrdinal)
                    ? null
                    : DateTime.SpecifyKind(reader.GetDateTime(createdOrdinal), DateTimeKind.Utc),
                IsCover = reader["IsCover"] is bool cover && cover,
                IsImage = !isLink && IsImageFile(fileUrl, contentType),
            };
        }

        private static bool IsImageFile(string fileUrl, string? contentType)
        {
            if (!string.IsNullOrWhiteSpace(contentType) && contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return true;
            return ImageExtensions.Contains(Path.GetExtension(fileUrl).ToLowerInvariant());
        }

        private static string? BuildDisplayName(string? firstName, string? lastName, string? userName)
        {
            var fullName = $"{firstName} {lastName}".Trim();
            if (!string.IsNullOrWhiteSpace(fullName)) return fullName;
            return string.IsNullOrWhiteSpace(userName) ? null : userName;
        }
    }
}
