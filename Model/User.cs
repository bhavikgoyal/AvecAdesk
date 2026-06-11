using System.ComponentModel.DataAnnotations.Schema;

namespace AvecADeskApi.Model
{
    public class User
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNo { get; set; }
        public int? UserRoleId { get; set; }
        public int? CompaniesId { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? ModifyOn { get; set; }
        public string Password { get; set; }
        [NotMapped]
        public string? RefreshToken { get; set; }
        [NotMapped]
        public DateTime? RefreshTokenExpiry { get; set; }
    }
}
