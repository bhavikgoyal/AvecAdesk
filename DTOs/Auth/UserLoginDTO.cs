namespace AvecADeskApi.DTOs.Auth
{
    public class UserLoginDTO
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public int UserRoleId { get; set; }
    }
}
