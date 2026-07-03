using AvecADeskApi.DTOs.Card;
using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AvecADeskApi.Repositories.TaskRepo
{
    public class CardRepository : ICardRepository
    {
        private readonly SqlDbHelper _db;
        private readonly LogHelper _logHelper;

        public CardRepository(SqlDbHelper db, LogHelper logHelper)
        {
            _db = db;
            _logHelper = logHelper;
        }

        public async Task<List<BoardColumnResponse>> GetBoardCardsAsync(
            string? searchText, int? assignedUserId, DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                var flatCards = await _db.ExecuteReaderListAsync(
                    "dbo.SP_GetBoardCards_new",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@SearchText", (object?)searchText ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@AssignedUserID", (object?)assignedUserId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);
                    },
                    MapCard);

               
                var columns = flatCards
                    .GroupBy(c => new { c.CardStatusID, c.StatusName })
                    .Select(g => new BoardColumnResponse
                    {
                        CardStatusID = g.Key.CardStatusID ?? 0,
                        StatusName = g.Key.StatusName ?? "Unknown",
                        Count = g.Count(),
                        Cards = g.ToList()
                    })
                    .OrderBy(c => c.CardStatusID)
                    .ToList();

                return columns;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardRepository)}.{nameof(GetBoardCardsAsync)}", ex);
                throw;
            }
        }

        public async Task<int> CreateCardAsync(CreateCardRequest request, int createdUserId)
        {
            try
            {
                var newCardIdParam = new SqlParameter("@NewCardId", SqlDbType.Int) { Direction = ParameterDirection.Output };

                await _db.ExecuteNonQueryAsync("dbo.SP_InsertCard", cmd =>
                {
                    cmd.Parameters.AddWithValue("@ListID", (object?)request.ListID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CardTitle", request.CardTitle);
                    cmd.Parameters.AddWithValue("@Description", request.Description ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Color", request.Color ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@DueDate", request.DueDate ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CreatedUserID", createdUserId);
                    cmd.Parameters.AddWithValue("@AssignedUserID", (object?)request.AssignedUserID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CardStatusID", request.CardStatusID);
                    cmd.Parameters.AddWithValue("@CPID", (object?)request.CPID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SheetType", request.SheetType ?? (object)DBNull.Value);
                    cmd.Parameters.Add(newCardIdParam);
                });

                return (int)newCardIdParam.Value;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardRepository)}.{nameof(CreateCardAsync)}", ex);
                throw;
            }
        }

        public async Task UpdateCardAsync(UpdateCardRequest request)
        {
            try
            {
                await _db.ExecuteNonQueryAsync("dbo.SP_UpdateCard", cmd =>
                {
                    cmd.Parameters.AddWithValue("@CardID", request.CardID);
                    cmd.Parameters.AddWithValue("@CardTitle", request.CardTitle);
                    cmd.Parameters.AddWithValue("@Description", request.Description ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Color", request.Color ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@DueDate", request.DueDate ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@AssignedUserID", (object?)request.AssignedUserID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CardStatusID", (object?)request.CardStatusID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CPID", (object?)request.CPID ?? DBNull.Value);
                });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardRepository)}.{nameof(UpdateCardAsync)}", ex);
                throw;
            }
        }

        
        public async Task MoveCardAsync(MoveCardRequest request)
        {
            try
            {
                await _db.ExecuteNonQueryAsync("dbo.SP_MoveCard", cmd =>
                {
                    cmd.Parameters.AddWithValue("@CardID", request.CardID);
                    cmd.Parameters.AddWithValue("@NewCardStatusID", request.NewCardStatusID);
                    cmd.Parameters.AddWithValue("@NewPosition", request.NewPosition);
                });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardRepository)}.{nameof(MoveCardAsync)}", ex);
                throw;
            }
        }

        public async Task DeleteCardAsync(int cardId)
        {
            try
            {
                await _db.ExecuteNonQueryAsync("dbo.SP_DeleteCard", cmd =>
                {
                    cmd.Parameters.AddWithValue("@CardID", cardId);
                });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardRepository)}.{nameof(DeleteCardAsync)}", ex);
                throw;
            }
        }

      
        private static CardResponse MapCard(SqlDataReader reader)
        {
            return new CardResponse
            {
                CardID = reader.GetInt32(reader.GetOrdinal("CardID")),
                ListID = reader["ListID"] is DBNull ? null : (int?)reader.GetInt32(reader.GetOrdinal("ListID")),
                CardTitle = reader["CardTitle"] as string,
                Description = reader["Description"] as string,
                Position = reader["Position"] is DBNull ? null : (int?)reader.GetInt32(reader.GetOrdinal("Position")),
                Color = reader["Color"] as string,
                DueDate = reader["DueDate"] is DBNull ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("DueDate")),
                CreatedUserID = reader["CreatedUserID"] is DBNull ? null : (int?)reader.GetInt32(reader.GetOrdinal("CreatedUserID")),
                CreatedUserName = reader["CreatedUserName"] as string,
                AssignedUserID = reader["AssignedUserID"] is DBNull ? null : (int?)reader.GetInt32(reader.GetOrdinal("AssignedUserID")),
                AssignedUserName = reader["AssignedUserName"] as string,
                IsArchived = reader["IsArchived"] is DBNull ? null : (bool?)reader.GetBoolean(reader.GetOrdinal("IsArchived")),
                CreatedAt = reader["CreatedAt"] is DBNull ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader["UpdatedAt"] is DBNull ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                CardStatusID = reader["CardStatusID"] is DBNull ? null : (int?)reader.GetInt32(reader.GetOrdinal("CardStatusID")),
                StatusName = reader["StatusName"] as string,
                CPID = reader["CPID"] is DBNull ? null : (int?)reader.GetInt32(reader.GetOrdinal("CPID")),
                PriorityName = reader["PriorityName"] as string,
                SheetType = reader["SheetType"] as string,
                ChecklistTotal = reader["ChecklistTotal"] is DBNull ? 0 : reader.GetInt32(reader.GetOrdinal("ChecklistTotal")),
                ChecklistCompleted = reader["ChecklistCompleted"] is DBNull ? 0 : reader.GetInt32(reader.GetOrdinal("ChecklistCompleted"))
            };
        }
    }
}

