using Microsoft.EntityFrameworkCore;

namespace AvecADeskApi.Model
{
    [Keyless]
    public class UserLoginResult
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public int? UserRoleId { get; set; }
        public bool? IsActive { get; set; }
    }
}