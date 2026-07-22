using AvecADeskApi.DTOs.Card;
using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using Microsoft.Data.SqlClient;

namespace AvecADeskApi.Repositories
{
    public class CardMemberRepository : ICardMemberRepository
    {
        private readonly SqlDbHelper _db;
        private readonly LogHelper _logHelper;

        public CardMemberRepository(SqlDbHelper db, LogHelper logHelper)
        {
            _db = db;
            _logHelper = logHelper;
        }

        public async Task<List<CardMemberResponse>> GetCardMembersAsync(int cardId)
        {
            try
            {
                return await _db.ExecuteReaderListAsync(
                    "dbo.SP_GetCardMembers",
                    cmd => cmd.Parameters.AddWithValue("@CardID", cardId),
                    reader => new CardMemberResponse
                    {
                        CardLabelID = reader.GetInt32(reader.GetOrdinal("CardLabelID")),
                        CardID = reader.GetInt32(reader.GetOrdinal("CardID")),
                        UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                        UserName = reader["UserName"] as string,
                        FirstName = reader["FirstName"] as string,
                        LastName = reader["LastName"] as string,
                    });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardMemberRepository)}.{nameof(GetCardMembersAsync)}", ex);
                throw;
            }
        }

        public async Task AddCardMemberAsync(int cardId, int userId)
        {
            try
            {
                await _db.ExecuteNonQueryAsync("dbo.SP_AddCardMember", cmd =>
                {
                    cmd.Parameters.AddWithValue("@CardID", cardId);
                    cmd.Parameters.AddWithValue("@UserID", userId);
                });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardMemberRepository)}.{nameof(AddCardMemberAsync)}", ex);
                throw;
            }
        }

        public async Task RemoveCardMemberAsync(int cardId, int userId)
        {
            try
            {
                await _db.ExecuteNonQueryAsync("dbo.SP_RemoveCardMember", cmd =>
                {
                    cmd.Parameters.AddWithValue("@CardID", cardId);
                    cmd.Parameters.AddWithValue("@UserID", userId);
                });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardMemberRepository)}.{nameof(RemoveCardMemberAsync)}", ex);
                throw;
            }
        }
    }
}