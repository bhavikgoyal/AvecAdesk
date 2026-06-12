namespace AvecADeskApi.Model.Reminder;

public class ReminderRuleUpdateRequest
{
    public string RuleType { get; set; } = string.Empty;
    public int TriggerAfterDays { get; set; }
    public int IntervalDays { get; set; }
    public int EmailTemplateId { get; set; }
    public bool IsActive { get; set; }
}
