using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.MailHistory;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AvecADeskApi.Repositories.MailHistory;

public class MailHistoryRepository : IMailHistoryRepository
{
    private readonly SqlDbHelper _db;
    private readonly LogHelper _logHelper;

    public MailHistoryRepository(SqlDbHelper db, LogHelper logHelper)
    {
        _db = db;
        _logHelper = logHelper;
    }

    public async Task AddAsync(MailHistoryEntry entry)
    {
        try
        {
            await _db.ExecuteNonQueryAsync("dbo.sp_InsertMailHistory", cmd =>
            {
                cmd.Parameters.Add("@MailType", SqlDbType.NVarChar, 100).Value = entry.MailType;
                cmd.Parameters.Add("@ToEmail", SqlDbType.NVarChar, 256).Value = (object?)entry.ToEmail ?? DBNull.Value;
                cmd.Parameters.Add("@IntendedToEmail", SqlDbType.NVarChar, 256).Value = (object?)entry.IntendedToEmail ?? DBNull.Value;
                cmd.Parameters.Add("@MailMode", SqlDbType.NVarChar, 20).Value = (object?)entry.MailMode ?? DBNull.Value;
                cmd.Parameters.Add("@FromEmail", SqlDbType.NVarChar, 256).Value = (object?)entry.FromEmail ?? DBNull.Value;
                cmd.Parameters.Add("@Subject", SqlDbType.NVarChar, 500).Value = (object?)entry.Subject ?? DBNull.Value;
                cmd.Parameters.Add("@BodyHtml", SqlDbType.NVarChar, -1).Value = (object?)entry.BodyHtml ?? DBNull.Value;
                cmd.Parameters.Add("@AttachmentName", SqlDbType.NVarChar, 260).Value = (object?)entry.AttachmentName ?? DBNull.Value;
                cmd.Parameters.Add("@Status", SqlDbType.NVarChar, 20).Value = entry.Status;
                cmd.Parameters.Add("@ErrorMessage", SqlDbType.NVarChar, 1000).Value = (object?)entry.ErrorMessage ?? DBNull.Value;
                cmd.Parameters.Add("@SentByUserId", SqlDbType.Int).Value = (object?)entry.SentByUserId ?? DBNull.Value;
                cmd.Parameters.Add("@SentByUserName", SqlDbType.NVarChar, 200).Value = (object?)entry.SentByUserName ?? DBNull.Value;
            });
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(MailHistoryRepository)}.{nameof(AddAsync)}", ex);
            throw;
        }
    }

    public async Task<MailHistoryPagedResult> GetPagedAsync(MailHistoryQuery query)
    {
        try
        {
            var result = new MailHistoryPagedResult();
            await _db.ExecuteReaderListAsync(
                "dbo.sp_GetMailHistory",
                cmd =>
                {
                    cmd.Parameters.Add("@Search", SqlDbType.NVarChar, 200).Value = (object?)query.Search ?? DBNull.Value;
                    cmd.Parameters.Add("@MailType", SqlDbType.NVarChar, 100).Value = (object?)query.MailType ?? DBNull.Value;
                    cmd.Parameters.Add("@Status", SqlDbType.NVarChar, 20).Value = (object?)query.Status ?? DBNull.Value;
                    cmd.Parameters.Add("@FromDate", SqlDbType.Date).Value = (object?)query.FromDate?.Date ?? DBNull.Value;
                    cmd.Parameters.Add("@ToDate", SqlDbType.Date).Value = (object?)query.ToDate?.Date ?? DBNull.Value;
                    cmd.Parameters.Add("@PageNumber", SqlDbType.Int).Value = query.PageNumber;
                    cmd.Parameters.Add("@PageSize", SqlDbType.Int).Value = query.PageSize;
                },
                reader =>
                {
                    result.TotalCount = reader.GetInt32(reader.GetOrdinal("TotalCount"));
                    result.SentCount = reader.GetInt32(reader.GetOrdinal("SentCount"));
                    result.FailedCount = reader.GetInt32(reader.GetOrdinal("FailedCount"));
                    result.SkippedCount = reader.GetInt32(reader.GetOrdinal("SkippedCount"));
                    var item = new MailHistoryListItem();
                    MapListFields(reader, item);
                    result.Items.Add(item);
                    return item;
                });
            return result;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(MailHistoryRepository)}.{nameof(GetPagedAsync)}", ex);
            throw;
        }
    }

    public async Task<MailHistoryDetail?> GetByIdAsync(int mailHistoryId)
    {
        try
        {
            return await _db.ExecuteReaderSingleAsync(
                "dbo.sp_GetMailHistoryById",
                cmd => cmd.Parameters.Add("@MailHistoryId", SqlDbType.Int).Value = mailHistoryId,
                reader =>
                {
                    var detail = new MailHistoryDetail();
                    MapListFields(reader, detail);
                    detail.FromEmail = ReadString(reader, "FromEmail");
                    detail.BodyHtml = ReadString(reader, "BodyHtml");
                    var userIdOrdinal = reader.GetOrdinal("SentByUserId");
                    detail.SentByUserId = reader.IsDBNull(userIdOrdinal) ? null : reader.GetInt32(userIdOrdinal);
                    return detail;
                });
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(MailHistoryRepository)}.{nameof(GetByIdAsync)}", ex);
            throw;
        }
    }

    public async Task<List<string>> GetMailTypesAsync()
    {
        try
        {
            return await _db.ExecuteReaderListAsync(
                "dbo.sp_GetMailHistoryTypes",
                _ => { },
                reader => reader.GetString(reader.GetOrdinal("MailType")));
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(MailHistoryRepository)}.{nameof(GetMailTypesAsync)}", ex);
            throw;
        }
    }

    private static void MapListFields(SqlDataReader reader, MailHistoryListItem item)
    {
        item.MailHistoryId = reader.GetInt32(reader.GetOrdinal("MailHistoryId"));
        item.MailType = ReadString(reader, "MailType") ?? string.Empty;
        item.ToEmail = ReadString(reader, "ToEmail");
        item.IntendedToEmail = ReadString(reader, "IntendedToEmail");
        item.MailMode = ReadString(reader, "MailMode");
        item.Subject = ReadString(reader, "Subject");
        item.AttachmentName = ReadString(reader, "AttachmentName");
        item.Status = ReadString(reader, "Status") ?? string.Empty;
        item.ErrorMessage = ReadString(reader, "ErrorMessage");
        item.SentByUserName = ReadString(reader, "SentByUserName");
        item.SentOn = reader.GetDateTime(reader.GetOrdinal("SentOn"));
    }

    private static string? ReadString(SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}
