using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AvecADeskApi.Repositories.Lists
{
    public class ListRepository : IListRepository
    {
        private readonly SqlDbHelper _db;
        private readonly LogHelper _logHelper;

        public ListRepository(SqlDbHelper db, LogHelper logHelper)
        {
            _db = db;
            _logHelper = logHelper;
        }

        public async Task<int> CreateListAsync(int boardId, string listName)
        {
            try
            {
                var newListIdParam = new SqlParameter("@NewListID", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };

                await _db.ExecuteNonQueryAsync("dbo.Sp_Lists_Insert", cmd =>
                {
                    cmd.Parameters.AddWithValue("@BoardID", boardId);
                    cmd.Parameters.AddWithValue("@ListName", listName.Trim());
                    cmd.Parameters.Add(newListIdParam);
                });

                return (int)newListIdParam.Value;
            }
            catch (Exception ex)
            {
                _logHelper.LogError(
                    $"{nameof(ListRepository)}.{nameof(CreateListAsync)}",
                    ex
                );

                throw;
            }
        }
    }
}