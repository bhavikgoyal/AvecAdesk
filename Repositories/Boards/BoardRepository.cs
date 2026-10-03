using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AvecADeskApi.Repositories.Boards
{
    public class BoardRepository : IBoardRepository
    {
        private readonly SqlDbHelper _db;
        private readonly LogHelper _logHelper;

        public BoardRepository(SqlDbHelper db, LogHelper logHelper)
        {
            _db = db;
            _logHelper = logHelper;
        }

        public async Task<int> CreateBoardAsync(string boardName, int createdByUserId)
        {
            try
            {
                var newBoardIdParam = new SqlParameter("@NewBoardID", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };

                await _db.ExecuteNonQueryAsync("dbo.Sp_Boards_Insert", cmd =>
                {
                    cmd.Parameters.AddWithValue("@BoardName", boardName.Trim());
                    cmd.Parameters.AddWithValue("@CreatedByUserID", createdByUserId);
                    cmd.Parameters.Add(newBoardIdParam);
                });

                return (int)newBoardIdParam.Value;
            }
            catch (Exception ex)
            {
                _logHelper.LogError(
                    $"{nameof(BoardRepository)}.{nameof(CreateBoardAsync)}",
                    ex
                );

                throw;
            }
        }

        public async Task<List<(int BoardID, string BoardName)>> GetBoardsByUserIdAsync(int userId)
        {
            try
            {
                return await _db.ExecuteReaderListAsync(
                    "dbo.Sp_Boards_GetByUserID",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@UserID", userId);
                    },
                    reader =>
                    (
                        reader.GetInt32(reader.GetOrdinal("BoardID")),
                        reader.GetString(reader.GetOrdinal("BoardName"))
                    )
                );
            }
            catch (Exception ex)
            {
                _logHelper.LogError(
                    $"{nameof(BoardRepository)}.{nameof(GetBoardsByUserIdAsync)}",
                    ex
                );

                throw;
            }
        }
    }
}