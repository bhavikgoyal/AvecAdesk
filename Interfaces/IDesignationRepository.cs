using AvecADeskApi.Model.Designation;

namespace AvecADeskApi.Interfaces;

public interface IDesignationRepository
{
    Task<IEnumerable<DesignationDto>> GetAllAsync();
}
