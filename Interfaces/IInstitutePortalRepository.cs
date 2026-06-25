using AvecADeskApi.Model.College;

namespace AvecADeskApi.Interfaces;

public interface IInstitutePortalRepository
{
    Task<InstitutePortalResponse?> GetPortalByInstituteNameAsync(
        string instituteName,
        string? query,
        string? level,
        string? intake,
        string? campus);
}
