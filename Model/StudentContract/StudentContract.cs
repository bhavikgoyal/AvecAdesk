namespace AvecADeskApi.Model.StudentContract;

public class StudentContract
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string ContractStatus { get; set; } = "Active";
    public DateTime? ContractStartDate { get; set; }
    public DateTime? ContractEndDate { get; set; }
    public string? ContractReferenceNo { get; set; }
    public string? ContractFileUrl { get; set; }
    public string? ContractFileName { get; set; }
    public string? Notes { get; set; }
}

public class StudentContractUpsertRequest
{
    public string? ContractStatus { get; set; }
    public DateTime? ContractStartDate { get; set; }
    public DateTime? ContractEndDate { get; set; }
    public string? ContractReferenceNo { get; set; }
    public string? ContractFileUrl { get; set; }
    public string? ContractFileName { get; set; }
    public string? Notes { get; set; }
}