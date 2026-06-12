namespace AvecADeskApi.Model.Commission;

public class CommissionRateResponse
{
    public int CommissionId { get; set; }
    public int VendorId { get; set; }
    public int? InstituteId { get; set; }
    public int? CourseId { get; set; }
    public string RateType { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}
