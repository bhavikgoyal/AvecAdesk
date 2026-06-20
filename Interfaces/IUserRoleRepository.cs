using AvecADeskApi.Model;

namespace AvecADeskApi.Interfaces
{
    public interface IUserRoleRepository
    {
        Task<List<UserRolesResponse>> GetRolesAsync();

        Task<List<UserRolesResponse>> GetCompaniesAsync();
    }
}
