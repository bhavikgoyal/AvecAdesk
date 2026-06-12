using AvecADeskApi.Model.Reminder;

namespace AvecADeskApi.Interfaces;

public interface IReminderRepository
{
    Task<List<ReminderRuleResponse>> GetReminderRulesAsync();
    Task<int> CreateReminderRuleAsync(ReminderRuleCreateRequest request);
    Task<bool> UpdateReminderRuleAsync(int ruleId, ReminderRuleUpdateRequest request);
    Task<List<ReminderLogResponse>> GetReminderLogsAsync();
    Task<int> TriggerReminderAsync(int ruleId, int referenceId);
}
