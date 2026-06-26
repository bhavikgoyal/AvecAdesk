namespace AvecADeskApi.Model.Vendor;

public class VendorPerformanceRequest
{
    public int VendorId { get; set; }
    public int? StudentsRecruitedLastYear { get; set; }
    public int? ExpectedStudentsNext12Months { get; set; }
    public decimal? VisaSuccessRate { get; set; }
}
