namespace AvecADeskApi.Model.DuePayment
{
    public class DuePaymentResponse
    {
        public int ScheduleId { get; set; }
        public int StudentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string EnrollmentNumber { get; set; } = string.Empty;
        public string EnrolmentStatus { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal DueAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
