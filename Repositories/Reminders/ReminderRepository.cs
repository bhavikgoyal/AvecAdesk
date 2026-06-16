using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Reminder;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AvecADeskApi.Repositories.Reminders;

public class ReminderRepository : IReminderRepository
{
    private readonly SqlDbHelper _db;
    private readonly LogHelper _logHelper;

    public ReminderRepository(SqlDbHelper db, LogHelper logHelper)
    {
        _db = db;
        _logHelper = logHelper;
    }

    public async Task<List<ReminderRuleResponse>> GetReminderRulesAsync()
    {
        try
        {
            return await _db.ExecuteReaderListAsync("sp_GetReminderRules", _ => { }, MapRule);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(ReminderRepository)}.{nameof(GetReminderRulesAsync)}", ex);
            throw;
        }
    }

    public async Task<int> CreateReminderRuleAsync(ReminderRuleCreateRequest request)
    {
        try
        {
            var ruleIdParam = new SqlParameter("@RuleId", SqlDbType.Int) { Direction = ParameterDirection.Output };
            await _db.ExecuteNonQueryAsync("sp_CreateReminderRule", cmd =>
            {
                cmd.Parameters.AddWithValue("@RuleType", request.RuleType);
                cmd.Parameters.AddWithValue("@TriggerAfterDays", request.TriggerAfterDays);
                cmd.Parameters.AddWithValue("@IntervalDays", request.IntervalDays);
                cmd.Parameters.AddWithValue("@EmailTemplateId", request.EmailTemplateId);
                cmd.Parameters.AddWithValue("@IsActive", request.IsActive);
                cmd.Parameters.Add(ruleIdParam);
            });
            return (int)ruleIdParam.Value;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(ReminderRepository)}.{nameof(CreateReminderRuleAsync)}", ex);
            throw;
        }
    }

    public async Task<bool> UpdateReminderRuleAsync(int ruleId, ReminderRuleUpdateRequest request)
    {
        try
        {
            var rowsParam = new SqlParameter("@RowsAffected", SqlDbType.Int) { Direction = ParameterDirection.Output };
            await _db.ExecuteNonQueryAsync("sp_UpdateReminderRule", cmd =>
            {
                cmd.Parameters.AddWithValue("@RuleId", ruleId);
                cmd.Parameters.AddWithValue("@RuleType", request.RuleType);
                cmd.Parameters.AddWithValue("@TriggerAfterDays", request.TriggerAfterDays);
                cmd.Parameters.AddWithValue("@IntervalDays", request.IntervalDays);
                cmd.Parameters.AddWithValue("@EmailTemplateId", request.EmailTemplateId);
                cmd.Parameters.AddWithValue("@IsActive", request.IsActive);
                cmd.Parameters.Add(rowsParam);
            });
            return (int)rowsParam.Value > 0;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(ReminderRepository)}.{nameof(UpdateReminderRuleAsync)}", ex);
            throw;
        }
    }

    public async Task<List<ReminderLogResponse>> GetReminderLogsAsync()
    {
        try
        {
            return await _db.ExecuteReaderListAsync("sp_GetReminderLogs", _ => { }, MapLog);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(ReminderRepository)}.{nameof(GetReminderLogsAsync)}", ex);
            throw;
        }
    }

    public async Task<int> TriggerReminderAsync(int ruleId, int referenceId)
    {
        try
        {
            var logIdParam = new SqlParameter("@LogId", SqlDbType.Int) { Direction = ParameterDirection.Output };
            await _db.ExecuteNonQueryAsync("sp_TriggerReminder", cmd =>
            {
                cmd.Parameters.AddWithValue("@RuleId", ruleId);
                cmd.Parameters.AddWithValue("@ReferenceId", referenceId);
                cmd.Parameters.Add(logIdParam);
            });
            return (int)logIdParam.Value;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(ReminderRepository)}.{nameof(TriggerReminderAsync)}", ex);
            throw;
        }
    }

    private static ReminderRuleResponse MapRule(SqlDataReader r) => new()
    {
        RuleId = r.GetInt32(r.GetOrdinal("RuleId")),
        RuleType = r.GetString(r.GetOrdinal("RuleType")),
        TriggerAfterDays = r.GetInt32(r.GetOrdinal("TriggerAfterDays")),
        IntervalDays = r.GetInt32(r.GetOrdinal("IntervalDays")),
        EmailTemplateId = r.GetInt32(r.GetOrdinal("EmailTemplateId")),
        IsActive = r.GetBoolean(r.GetOrdinal("IsActive"))
    };

    private static ReminderLogResponse MapLog(SqlDataReader r) => new()
    {
        LogId = r.GetInt32(r.GetOrdinal("LogId")),
        RuleId = r.GetInt32(r.GetOrdinal("RuleId")),
        ReferenceId = r.GetInt32(r.GetOrdinal("ReferenceId")),
        SentAt = r.GetDateTime(r.GetOrdinal("SentAt")),
        EmailStatus = r.GetString(r.GetOrdinal("EmailStatus"))
    };

    public async Task<ReminderStatsResponse> GetReminderStatsAsync()
    {
        try
        {
            return await _db.ExecuteReaderSingleAsync("sp_GetReminderStats", _ => { }, r => new ReminderStatsResponse
            {
                Active = r.GetInt32(r.GetOrdinal("ActiveRules")),
                SentToday = r.GetInt32(r.GetOrdinal("SentToday")),
                Paused = r.GetInt32(r.GetOrdinal("PausedRules")),
                Failed = r.GetInt32(r.GetOrdinal("FailedCount"))
            });
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(ReminderRepository)}.{nameof(GetReminderStatsAsync)}", ex);
            throw;
        }
    }
}
