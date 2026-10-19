namespace AvecADeskApi.Model.Student;

public class StudentCancelDetailRequest
{
    public bool IsActive { get; set; } = true;
    public DateTime? CancelDate { get; set; }
    public string? CancelReason { get; set; }
    public DateTime? ActiveDate { get; set; }
}

public class StudentCancelDetailResponse
{
    public int StudentId { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CancelDate { get; set; }
    public string? CancelReason { get; set; }
    public DateTime? ActiveDate { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
