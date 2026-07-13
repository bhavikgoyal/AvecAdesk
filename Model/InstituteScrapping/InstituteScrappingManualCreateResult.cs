namespace AvecADeskApi.Model.InstituteScrapping;

public class InstituteScrappingManualCreateResult
{
    public int ScrappingId { get; set; }
    public int CourseId { get; set; }
    public int InstituteId { get; set; }
    public InstituteScrappingResponse? Record { get; set; }
}
