using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model; 
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace AvecADeskApi.Repositories
{
    public class CardStatusRepository : ICardStatusRepository
    {
        private readonly SqlDbHelper _db;
        private readonly LogHelper _logHelper;

        public CardStatusRepository(SqlDbHelper db, LogHelper logHelper)
        {
            _db = db;
            _logHelper = logHelper;
        }


        public async Task<List<CardStatusResponse>> GetCardStatusesAsync()
        {
            try
            {
                return await _db.ExecuteReaderListAsync(
                    "dbo.sp_GetCardStatuses",
                    cmd => {},
                    MapCardStatus
                );
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardStatusRepository)}.{nameof(GetCardStatusesAsync)}", ex);
                throw;
            }
        }

        public async Task<CardStatusResponse> CreateCardStatusAsync(string statusName)
        {
            try
            {
                var newIdParam = new SqlParameter("@NewCardStatusID", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };

                await _db.ExecuteNonQueryAsync("dbo.SP_InsertCardStatus", cmd =>
                {
                    cmd.Parameters.AddWithValue("@StatusName", statusName.Trim());
                    cmd.Parameters.Add(newIdParam);
                });

                return new CardStatusResponse
                {
                    CardStatusID = (int)newIdParam.Value,
                    StatusName = statusName.Trim()
                };
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(CardStatusRepository)}.{nameof(CreateCardStatusAsync)}", ex);
                throw;
            }
        }

       
        private static CardStatusResponse MapCardStatus(SqlDataReader reader)
        {
            return new CardStatusResponse
            {
                CardStatusID = reader.GetInt32(reader.GetOrdinal("CardStatusID")),
                
                StatusName = reader.IsDBNull(reader.GetOrdinal("StatusName")) ? null : reader.GetString(reader.GetOrdinal("StatusName"))
            };
        }
    }
}