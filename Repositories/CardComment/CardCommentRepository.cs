using AvecADeskApi.DTOs.Card;
using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using Microsoft.Data.SqlClient;

namespace AvecADeskApi.Repositories.CardComment
{
    public class CardCommentRepository : ICardCommentRepository
    {
        private readonly SqlDbHelper _db;
        private readonly LogHelper _logHelper;

        public CardCommentRepository(SqlDbHelper db, LogHelper logHelper)
        {
            _db = db;
            _logHelper = logHelper;
        }

        public async Task<List<CardCommentResponse>> GetByCardIdAsync(int cardId)
        {
            try
            {
                return await _db.ExecuteReaderListAsync(
                    "dbo.SP_GetCardCommentsForCard",
                    cmd => cmd.Parameters.AddWithValue("@CardID", cardId),
                    MapComment);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardCommentRepository)}.{nameof(GetByCardIdAsync)}", ex);
                throw;
            }
        }

        public async Task<CardCommentResponse?> CreateAsync(int cardId, int userId, string commentText)
        {
            try
            {
                return await _db.ExecuteReaderSingleAsync(
                    "dbo.SP_InsertCardComment",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@CardID", cardId);
                        cmd.Parameters.AddWithValue("@UserID", userId);
                        cmd.Parameters.AddWithValue("@CommentText", commentText);
                    },
                    MapComment);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardCommentRepository)}.{nameof(CreateAsync)}", ex);
                throw;
            }
        }

        public async Task<CardCommentResponse?> UpdateAsync(int commentId, int userId, string commentText)
        {
            try
            {
                return await _db.ExecuteReaderSingleAsync(
                    "dbo.SP_UpdateCardComment",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@CommentID", commentId);
                        cmd.Parameters.AddWithValue("@UserID", userId);
                        cmd.Parameters.AddWithValue("@CommentText", commentText);
                    },
                    MapComment);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardCommentRepository)}.{nameof(UpdateAsync)}", ex);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int commentId, int userId)
        {
            try
            {
                var result = await _db.ExecuteScalarAsync(
                    "dbo.SP_DeleteCardComment",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@CommentID", commentId);
                        cmd.Parameters.AddWithValue("@UserID", userId);
                    });

                return result != null && result != DBNull.Value && Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardCommentRepository)}.{nameof(DeleteAsync)}", ex);
                throw;
            }
        }

        public async Task<List<CardActivityResponse>> GetActivityByCardIdAsync(int cardId)
        {
            try
            {
                return await _db.ExecuteReaderListAsync(
                    "dbo.SP_GetCardActivityForCard",
                    cmd => cmd.Parameters.AddWithValue("@CardID", cardId),
                    reader => new CardActivityResponse
                    {
                        ActivityID = reader.GetInt32(reader.GetOrdinal("ActivityID")),
                        UserID = reader["UserID"] is DBNull ? null : reader.GetInt32(reader.GetOrdinal("UserID")),
                        DisplayName = TrelloMemberName(reader) ?? BuildDisplayName(
                            reader["FirstName"] as string,
                            reader["LastName"] as string,
                            reader["UserName"] as string),
                        ActivityType = reader["ActivityType"] as string,
                        ActivityDescription = reader["ActivityDescription"] as string,
                        OldValue = reader["OldValue"] as string,
                        NewValue = reader["NewValue"] as string,
                        CreatedAt = AsUtc(reader, "CreatedAt"),
                    });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardCommentRepository)}.{nameof(GetActivityByCardIdAsync)}", ex);
                throw;
            }
        }

        public async Task LogActivityAsync(LogCardActivityRequest request, int userId)
        {
            try
            {
                await _db.ExecuteScalarAsync(
                    "dbo.SP_InsertCardActivity",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@CardID", request.CardID);
                        cmd.Parameters.AddWithValue("@UserID", userId);
                        cmd.Parameters.AddWithValue("@ActivityType", request.ActivityType);
                        cmd.Parameters.AddWithValue("@ActivityDescription", (object?)request.Description ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@OldValue", (object?)request.OldValue ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@NewValue", (object?)request.NewValue ?? DBNull.Value);
                    });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardCommentRepository)}.{nameof(LogActivityAsync)}", ex);
                throw;
            }
        }

        private static string? TrelloMemberName(SqlDataReader reader)
        {
            for (var i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i) == "TrelloMemberName")
                    return reader.IsDBNull(i) || string.IsNullOrWhiteSpace(reader.GetString(i)) ? null : reader.GetString(i);
            }
            return null;
        }

        private static CardCommentResponse MapComment(SqlDataReader reader)
        {
            var firstName = reader["FirstName"] as string;
            var lastName = reader["LastName"] as string;
            var userName = reader["UserName"] as string;

            return new CardCommentResponse
            {
                CommentID = reader.GetInt32(reader.GetOrdinal("CommentID")),
                CardID = reader.GetInt32(reader.GetOrdinal("CardID")),
                UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                UserName = userName,
                FirstName = firstName,
                LastName = lastName,
                DisplayName = BuildDisplayName(firstName, lastName, userName),
                CommentText = reader["CommentText"] as string ?? string.Empty,
                CreatedAt = AsUtc(reader, "CreatedAt"),
                IsEdited = reader["IsEdited"] is bool edited && edited,
            };
        }

        // DB me dates UTC hai - Kind set karne se JSON me "Z" aata hai aur browser local time dikhata hai
        private static DateTime? AsUtc(SqlDataReader reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal)
                ? null
                : DateTime.SpecifyKind(reader.GetDateTime(ordinal), DateTimeKind.Utc);
        }

        private static string BuildDisplayName(string? firstName, string? lastName, string? userName)
        {
            var fullName = $"{firstName} {lastName}".Trim();
            if (!string.IsNullOrWhiteSpace(fullName)) return fullName;
            return string.IsNullOrWhiteSpace(userName) ? "Unknown user" : userName;
        }
    }
}
