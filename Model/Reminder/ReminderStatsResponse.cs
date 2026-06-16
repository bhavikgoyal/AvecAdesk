namespace AvecADeskApi.Model.Reminder;

public class ReminderStatsResponse
{
    public int Active { get; set; }
    public int SentToday { get; set; }
    public int Paused { get; set; }
    public int Failed { get; set; }
}