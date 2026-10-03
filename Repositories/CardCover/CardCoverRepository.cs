using AvecADeskApi.DTOs.Card;
using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using Microsoft.Data.SqlClient;

namespace AvecADeskApi.Repositories.CardCover
{
    public class CardCoverRepository : ICardCoverRepository
    {
        private readonly SqlDbHelper _db;
        private readonly LogHelper _logHelper;

        public CardCoverRepository(SqlDbHelper db, LogHelper logHelper)
        {
            _db = db;
            _logHelper = logHelper;
        }

        public async Task<List<CardCoverColorResponse>> GetColorsAsync()
        {
            try
            {
                return await _db.ExecuteReaderListAsync(
                    "dbo.SP_GetCardCoverColors",
                    _ => { },
                    reader => new CardCoverColorResponse
                    {
                        ColorKey = reader["ColorKey"] as string ?? string.Empty,
                        ColorName = reader["ColorName"] as string ?? string.Empty,
                        HexCode = reader["HexCode"] as string ?? string.Empty,
                        TextHexCode = reader["TextHexCode"] as string ?? string.Empty,
                        DisplayOrder = reader.GetInt32(reader.GetOrdinal("DisplayOrder")),
                    });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardCoverRepository)}.{nameof(GetColorsAsync)}", ex);
                throw;
            }
        }

        public async Task<CardCoverResponse?> GetByCardIdAsync(int cardId)
        {
            try
            {
                return await _db.ExecuteReaderSingleAsync(
                    "dbo.SP_GetCardCoverByCardId",
                    cmd => cmd.Parameters.AddWithValue("@CardID", cardId),
                    MapCover);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardCoverRepository)}.{nameof(GetByCardIdAsync)}", ex);
                throw;
            }
        }

        public async Task<Dictionary<int, CardCoverResponse>> GetByCardIdsAsync(IEnumerable<int> cardIds)
        {
            var idList = cardIds.Distinct().ToList();
            if (idList.Count == 0) return new Dictionary<int, CardCoverResponse>();

            try
            {
                var covers = await _db.ExecuteReaderListAsync(
                    "dbo.SP_GetCardCoversByCardIds",
                    cmd => cmd.Parameters.AddWithValue("@CardIDs", string.Join(",", idList)),
                    MapCover);

                return covers
                    .GroupBy(c => c.CardID)
                    .ToDictionary(g => g.Key, g => g.First());
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardCoverRepository)}.{nameof(GetByCardIdsAsync)}", ex);
                throw;
            }
        }

        public async Task<CardCoverResponse?> SaveAsync(SaveCardCoverRequest request, int? userId)
        {
            try
            {
                return await _db.ExecuteReaderSingleAsync(
                    "dbo.SP_UpsertCardCover",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@CardID", request.CardID);
                        cmd.Parameters.AddWithValue("@CoverColor", (object?)request.Color ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CoverImageUrl", (object?)request.ImageUrl ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CoverSize", (object?)request.Size ?? "normal");
                        cmd.Parameters.AddWithValue("@CoverBrightness", (object?)request.Brightness ?? "light");
                        cmd.Parameters.AddWithValue("@UserID", (object?)userId ?? DBNull.Value);
                    },
                    MapCover);
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardCoverRepository)}.{nameof(SaveAsync)}", ex);
                throw;
            }
        }

        public async Task<bool> RemoveAsync(int cardId)
        {
            try
            {
                var result = await _db.ExecuteScalarAsync(
                    "dbo.SP_RemoveCardCover",
                    cmd => cmd.Parameters.AddWithValue("@CardID", cardId));

                return result != null && result != DBNull.Value && Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardCoverRepository)}.{nameof(RemoveAsync)}", ex);
                throw;
            }
        }

        private static CardCoverResponse MapCover(SqlDataReader reader)
        {
            return new CardCoverResponse
            {
                CardID = reader.GetInt32(reader.GetOrdinal("CardID")),
                Color = reader["CoverColor"] as string,
                HexCode = reader["CoverHexCode"] as string,
                TextHexCode = reader["CoverTextHexCode"] as string,
                ImageUrl = reader["CoverImageUrl"] as string,
                Size = reader["CoverSize"] as string ?? "normal",
                Brightness = reader["CoverBrightness"] as string ?? "light",
            };
        }
    }
}
