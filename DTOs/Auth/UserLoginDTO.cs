namespace AvecADeskApi.DTOs.Auth
{
    public class UserLoginDTO
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public int UserRoleId { get; set; }
        public string UserRoleName { get; set; }
    }

    public class StudentLoginDTO
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string MobileNumber { get; set; } = "";
    }
}