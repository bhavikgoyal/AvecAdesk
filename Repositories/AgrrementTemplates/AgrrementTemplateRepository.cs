using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.AgrrementTemplate;
using Microsoft.Data.SqlClient;

namespace AvecADeskApi.Repositories.AgrrementTemplates;

public class AgrrementTemplateRepository : IAgrrementTemplateRepository
{
    private readonly SqlDbHelper _db;
    private readonly LogHelper _logHelper;

    public AgrrementTemplateRepository(SqlDbHelper db, LogHelper logHelper)
    {
        _db = db;
        _logHelper = logHelper;
    }

    public async Task<List<AgrrementTemplateResponse>> GetAgrrementTemplatesAsync()
    {
        try
        {
            return await _db.ExecuteReaderListAsync("sp_AgrrementTemplate_GetAll", _ => { }, MapTemplate);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(AgrrementTemplateRepository)}.{nameof(GetAgrrementTemplatesAsync)}", ex);
            throw;
        }
    }

    public async Task<AgrrementTemplateResponse?> GetAgrrementTemplateByIdAsync(int templateId)
    {
        try
        {
            return await _db.ExecuteReaderSingleAsync("sp_AgrrementTemplate_GetById",
                cmd => cmd.Parameters.AddWithValue("@TemplateId", templateId), MapTemplate);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(AgrrementTemplateRepository)}.{nameof(GetAgrrementTemplateByIdAsync)}", ex);
            throw;
        }
    }

    public async Task<int> CreateAgrrementTemplateAsync(AgrrementTemplateCreateRequest request)
    {
        try
        {
            var result = await _db.ExecuteScalarAsync("sp_AgrrementTemplate_Insert", cmd =>
            {
                cmd.Parameters.AddWithValue("@TemplateName", request.TemplateName);
                cmd.Parameters.AddWithValue("@AgreementType", request.AgreementType);
                cmd.Parameters.AddWithValue("@BodyHtml", request.BodyHtml);
                cmd.Parameters.AddWithValue("@IsActive", request.IsActive);
                cmd.Parameters.AddWithValue("@CreatedByUserId", request.CreatedByUserId);
            });

            return result is null ? 0 : Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(AgrrementTemplateRepository)}.{nameof(CreateAgrrementTemplateAsync)}", ex);
            throw;
        }
    }

    public async Task<bool> UpdateAgrrementTemplateAsync(int templateId, AgrrementTemplateUpdateRequest request)
    {
        try
        {
            var result = await _db.ExecuteScalarAsync("sp_AgrrementTemplate_Update", cmd =>
            {
                cmd.Parameters.AddWithValue("@TemplateId", templateId);
                cmd.Parameters.AddWithValue("@TemplateName", request.TemplateName);
                cmd.Parameters.AddWithValue("@AgreementType", request.AgreementType);
                cmd.Parameters.AddWithValue("@BodyHtml", request.BodyHtml);
                cmd.Parameters.AddWithValue("@IsActive", request.IsActive);
                cmd.Parameters.AddWithValue("@CreatedByUserId", request.CreatedByUserId);
            });

            return result is not null && Convert.ToInt32(result) > 0;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(AgrrementTemplateRepository)}.{nameof(UpdateAgrrementTemplateAsync)}", ex);
            throw;
        }
    }

    public async Task<bool> DeleteAgrrementTemplateAsync(int templateId)
    {
        try
        {
            var result = await _db.ExecuteScalarAsync("sp_AgrrementTemplate_Delete", cmd =>
            {
                cmd.Parameters.AddWithValue("@TemplateId", templateId);
            });

            return result != null && Convert.ToInt32(result) > 0;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(AgrrementTemplateRepository)}.{nameof(DeleteAgrrementTemplateAsync)}", ex);
            throw;
        }
    }

    private static AgrrementTemplateResponse MapTemplate(SqlDataReader r) => new()
    {
        TemplateId = r.GetInt32(r.GetOrdinal("TemplateId")),
        TemplateName = r.GetString(r.GetOrdinal("TemplateName")),
        AgreementType = r.GetString(r.GetOrdinal("AgreementType")),
        BodyHtml = r.GetString(r.GetOrdinal("BodyHtml")),
        IsActive = r.GetBoolean(r.GetOrdinal("IsActive")),
        CreatedByUserId = r.GetInt32(r.GetOrdinal("CreatedByUserId")),
        CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")),
        UpdatedAt = r.IsDBNull(r.GetOrdinal("UpdatedAt")) ? null : r.GetDateTime(r.GetOrdinal("UpdatedAt"))
    };
}
