namespace AvecADeskApi.Model.Reminder;

public class ReminderLogResponse
{
    public int LogId { get; set; }
    public int RuleId { get; set; }
    public int ReferenceId { get; set; }
    public DateTime SentAt { get; set; }
    public string EmailStatus { get; set; } = string.Empty;
}
