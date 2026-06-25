namespace AvecADeskApi.Model.College;

public class InstitutePortalResponse
{
    public InstitutePortalProfileResponse? Profile { get; set; }
    public List<InstitutePortalCourseResponse> Courses { get; set; } = new();
}
