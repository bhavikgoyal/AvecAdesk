namespace AvecADeskApi.Model.PaymentSchedule
{
    public class PaymentScheduleBulkItemResult
    {
        public int ScheduleId { get; set; }
        public bool Success { get; set; }
        public string? Error { get; set; }
    }

    public class PaymentScheduleBulkStatusResult
    {
        public int UpdatedCount { get; set; }
        public int FailedCount { get; set; }
        public List<PaymentScheduleBulkItemResult> Items { get; set; } = new();
    }
}