namespace AvecADeskApi.Model.InstituteContract;

public class InstituteContractDto
{
    public int Id { get; set; }
    public int InstituteId { get; set; }
    public string ContractStatus { get; set; } = "Active";
    public DateTime? ContractStartDate { get; set; }
    public DateTime? ContractEndDate { get; set; }
    public string? ContractReferenceNo { get; set; }
    public string? ContractFileUrl { get; set; }
    public string? Notes { get; set; }
}

public class InstituteContractUpsertRequest
{
    public string ContractStatus { get; set; } = "Active";
    public DateTime? ContractStartDate { get; set; }
    public DateTime? ContractEndDate { get; set; }
    public string? ContractReferenceNo { get; set; }
    public string? ContractFileUrl { get; set; }
    public string? Notes { get; set; }
}