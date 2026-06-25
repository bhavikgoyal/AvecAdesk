using AvecADeskApi.Model.UserPassword;

namespace AvecADeskApi.Interfaces
{
    public interface IUserRepository
    {
        Task ChangePasswordAsync(UserChangePasswordRequest request);
    }
}
