namespace AvecADeskApi.Model.PaymentSchedule;

public class PaymentScheduleSummaryResponse
{
    public int ActiveStudents { get; set; }
    public decimal CollectedTotal { get; set; }
    public decimal OutstandingTotal { get; set; }
    public decimal OverdueTotal { get; set; }
    public int OverdueCount { get; set; }
}
