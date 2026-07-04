using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.EmailTemplate;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AvecADeskApi.Repositories.EmailTemplates;

public class EmailTemplateRepository : IEmailTemplateRepository
{
    private readonly SqlDbHelper _db;
    private readonly LogHelper _logHelper;

    public EmailTemplateRepository(SqlDbHelper db, LogHelper logHelper)
    {
        _db = db;
        _logHelper = logHelper;
    }

    public async Task<bool> DeleteEmailTemplateAsync(int templateId)
    {
        try
        {
            await _db.ExecuteNonQueryAsync("sp_EmailTemplates_Delete", cmd =>
            {
                cmd.Parameters.AddWithValue("@TemplateId", templateId);
            });
            // Stored procedure uses SET NOCOUNT ON which may make ExecuteNonQuery return -1.
            // Treat successful execution (no exception) as deleted.
            return true;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(EmailTemplateRepository)}.{nameof(DeleteEmailTemplateAsync)}", ex);
            throw;
        }
    }

    public async Task<List<EmailTemplateResponse>> GetEmailTemplatesAsync()
    {
        try
        {
            return await _db.ExecuteReaderListAsync("sp_GetEmailTemplates", _ => { }, MapTemplate);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(EmailTemplateRepository)}.{nameof(GetEmailTemplatesAsync)}", ex);
            throw;
        }
    }

    public async Task<EmailTemplateResponse?> GetEmailTemplateByIdAsync(int templateId)
    {
        try
        {
            return await _db.ExecuteReaderSingleAsync("sp_GetEmailTemplateById",
                cmd => cmd.Parameters.AddWithValue("@TemplateId", templateId), MapTemplate);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(EmailTemplateRepository)}.{nameof(GetEmailTemplateByIdAsync)}", ex);
            throw;
        }
    }

    public async Task<int> CreateEmailTemplateAsync(EmailTemplateCreateRequest request)
    {
        try
        {
            var templateIdParam = new SqlParameter("@TemplateId", SqlDbType.Int) { Direction = ParameterDirection.Output };
            await _db.ExecuteNonQueryAsync("sp_CreateEmailTemplate", cmd =>
            {
                cmd.Parameters.AddWithValue("@Name", request.Name);
                cmd.Parameters.AddWithValue("@Subject", request.Subject);
                cmd.Parameters.AddWithValue("@BodyHtml", request.BodyHtml);
                cmd.Parameters.AddWithValue("@Category", request.Category);
                cmd.Parameters.Add(templateIdParam);
            });
            return (int)templateIdParam.Value;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(EmailTemplateRepository)}.{nameof(CreateEmailTemplateAsync)}", ex);
            throw;
        }
    }

    public async Task<bool> UpdateEmailTemplateAsync(int templateId, EmailTemplateUpdateRequest request)
    {
        try
        {
            var rowsParam = new SqlParameter("@RowsAffected", SqlDbType.Int) { Direction = ParameterDirection.Output };
            await _db.ExecuteNonQueryAsync("sp_UpdateEmailTemplate", cmd =>
            {
                cmd.Parameters.AddWithValue("@TemplateId", templateId);
                cmd.Parameters.AddWithValue("@Name", request.Name);
                cmd.Parameters.AddWithValue("@Subject", request.Subject);
                cmd.Parameters.AddWithValue("@BodyHtml", request.BodyHtml);
                cmd.Parameters.AddWithValue("@Category", request.Category);
                cmd.Parameters.Add(rowsParam);
            });
            return (int)rowsParam.Value > 0;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(EmailTemplateRepository)}.{nameof(UpdateEmailTemplateAsync)}", ex);
            throw;
        }
    }

    private static EmailTemplateResponse MapTemplate(SqlDataReader r) => new()
    {
        TemplateId = r.GetInt32(r.GetOrdinal("TemplateId")),
        Name = r.GetString(r.GetOrdinal("Name")),
        Subject = r.GetString(r.GetOrdinal("Subject")),
        BodyHtml = r.GetString(r.GetOrdinal("BodyHtml")),
        Category = r.GetString(r.GetOrdinal("Category"))
    };
}
