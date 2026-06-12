using AvecADeskApi.Model.Student;

namespace AvecADeskApi.Model.Aih;

public class AihConvertResponse
{
    public AihResponse Interest { get; set; } = new();
    public StudentResponse Student { get; set; } = new();
}
