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
        private readonly ILabelRepository _labelRepo;
        private readonly ICardCoverRepository _coverRepo;
        private readonly ICardAttachmentRepository _attachmentRepo;
        public CardRepository(
            SqlDbHelper db,
            LogHelper logHelper,
            ILabelRepository labelRepo,
            ICardCoverRepository coverRepo,
            ICardAttachmentRepository attachmentRepo)
        {
            _db = db;
            _logHelper = logHelper;
            _labelRepo = labelRepo;
            _coverRepo = coverRepo;
            _attachmentRepo = attachmentRepo;
        }

        // Cover optional hai - cover load fail ho to bhi board load hona chahiye
        private async Task AttachCoversAsync(List<CardResponse> cards)
        {
            try
            {
                var coversByCard = await _coverRepo.GetByCardIdsAsync(cards.Select(c => c.CardID));
                foreach (var card in cards)
                {
                    if (coversByCard.TryGetValue(card.CardID, out var cover))
                    {
                        card.Cover = cover;
                    }
                }
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardRepository)}.{nameof(AttachCoversAsync)}", ex);
            }

            // Attachment count bhi optional - script na chali ho to board phir bhi load ho
            try
            {
                var counts = await _attachmentRepo.GetCountsByCardIdsAsync(cards.Select(c => c.CardID));
                foreach (var card in cards)
                {
                    if (counts.TryGetValue(card.CardID, out var count))
                    {
                        card.AttachmentCount = count;
                    }
                }
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardRepository)}.{nameof(AttachCoversAsync)}.AttachmentCounts", ex);
            }
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

                // NEW: saare cards ke labels ek hi batch call me
                var cardIds = flatCards.Select(c => c.CardID).ToList();
                var labelsByCard = await _labelRepo.GetByCardIdsAsync(cardIds);

                foreach (var card in flatCards)
                {
                    if (labelsByCard.TryGetValue(card.CardID, out var cardLabels))
                    {
                        card.Labels = cardLabels;
                    }
                }

                await AttachCoversAsync(flatCards);

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

        public async Task<List<BoardColumnResponse>> GetMyAssignedBoardCardsAsync(
            int assignedUserId, string? searchText, DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                var flatCards = await _db.ExecuteReaderListAsync(
                    "dbo.SP_GetMyAssignedBoardCards",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@AssignedUserID", assignedUserId);
                        cmd.Parameters.AddWithValue("@SearchText", (object?)searchText ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);
                    },
                    MapCard);

                var cardIds = flatCards.Select(c => c.CardID).ToList();
                var labelsByCard = await _labelRepo.GetByCardIdsAsync(cardIds);

                foreach (var card in flatCards)
                {
                    if (labelsByCard.TryGetValue(card.CardID, out var cardLabels))
                    {
                        card.Labels = cardLabels;
                    }
                }

                await AttachCoversAsync(flatCards);

                return flatCards
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
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardRepository)}.{nameof(GetMyAssignedBoardCardsAsync)}", ex);
                throw;
            }
        }

        public async Task<List<CardResponse>> GetCardsByBoardIdAsync(
    int boardId,
    string? searchText,
    int? assignedUserId,
    DateTime? fromDate,
    DateTime? toDate)
        {
            try
            {
                var flatCards = await _db.ExecuteReaderListAsync(
                    "dbo.Sp_Cards_GetByBoardID",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@BoardID", boardId);
                        cmd.Parameters.AddWithValue(
                            "@SearchText",
                            (object?)searchText ?? DBNull.Value
                        );
                        cmd.Parameters.AddWithValue(
                            "@AssignedUserID",
                            (object?)assignedUserId ?? DBNull.Value
                        );
                        cmd.Parameters.AddWithValue(
                            "@FromDate",
                            (object?)fromDate ?? DBNull.Value
                        );
                        cmd.Parameters.AddWithValue(
                            "@ToDate",
                            (object?)toDate ?? DBNull.Value
                        );
                    },
                    MapCard
                );

                var cardIds = flatCards.Select(c => c.CardID).ToList();
                var labelsByCard = await _labelRepo.GetByCardIdsAsync(cardIds);

                foreach (var card in flatCards)
                {
                    if (labelsByCard.TryGetValue(card.CardID, out var cardLabels))
                    {
                        card.Labels = cardLabels;
                    }
                }

                await AttachCoversAsync(flatCards);

                return flatCards;
            }
            catch (Exception ex)
            {
                _logHelper.LogError(
                    $"{nameof(CardRepository)}.{nameof(GetCardsByBoardIdAsync)}",
                    ex
                );

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
                    cmd.Parameters.AddWithValue("@BoardID", (object?)request.BoardID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CardTitle", request.CardTitle);
                    cmd.Parameters.AddWithValue("@Description", request.Description ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Color", request.Color ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@DueDate", request.DueDate ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CreatedUserID", createdUserId);
                    cmd.Parameters.AddWithValue("@AssignedUserID", (object?)request.AssignedUserID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CardStatusID", (object?)request.CardStatusID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CPID", (object?)request.CPID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SheetType", request.SheetType ?? (object)DBNull.Value);
                    cmd.Parameters.Add(newCardIdParam);
                });

                var newCardId = (int)newCardIdParam.Value;
                var mappedUserId = request.AssignedUserID ?? createdUserId;
                await _db.ExecuteNonQueryAsync("dbo.SP_InsertCardUserMapping", cmd =>
                {
                    cmd.Parameters.AddWithValue("@CardId", newCardId);
                    cmd.Parameters.AddWithValue("@UserId", mappedUserId);
                });

                return newCardId;
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

        public async Task<MoveCardToListResponse?> MoveCardToListAsync(MoveCardToListRequest request)
        {
            try
            {
                return await _db.ExecuteReaderSingleAsync(
                    "dbo.SP_MoveCardToList",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@CardID", request.CardID);
                        cmd.Parameters.AddWithValue("@ListID", request.ListID);
                        cmd.Parameters.AddWithValue("@Position", request.Position);
                    },
                    reader => new MoveCardToListResponse
                    {
                        CardID = reader.GetInt32(reader.GetOrdinal("CardID")),
                        ListID = reader.GetInt32(reader.GetOrdinal("ListID")),
                        BoardID = reader.GetInt32(reader.GetOrdinal("BoardID")),
                        Position = reader.GetInt32(reader.GetOrdinal("Position"))
                    });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardRepository)}.{nameof(MoveCardToListAsync)}", ex);
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
                BoardID = reader["BoardID"] is DBNull ? null : (int?)reader.GetInt32(reader.GetOrdinal("BoardID")),
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

