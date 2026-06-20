using AvecADeskApi.Model.UserResponse;

public interface IMembersRepository
{
    Task<List<UserResponse>> GetAllUsersAsync(int loginUserId, string roleName);

    Task<int> CreateUserAsync(UserResponse request);

    Task<bool> UpdateUserAsync(UserResponse request);

    Task<bool> DeleteUserAsync(int userId);

    Task<UserResponse?> GetUserByUserNameAsync(string userName);

    Task<bool> ResignMemberAsync(int userId, DateTime resignDate);
}