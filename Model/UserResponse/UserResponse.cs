namespace AvecADeskApi.Model.UserResponse
{
    public class UserResponse
    {
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public bool Active { get; set; }
        public string? PhoneNo { get; set; }
        public int UserRoleId { get; set; }
        public string? RoleName { get; set; }
        public int CompaniesId { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? AvatarBase64 { get; set; }
        // Null means "leave board access as it is"; an empty list removes every board.
        public List<int>? BoardIds { get; set; }
    }

    public class UserBoardAccessResponse
    {
        public int UserBoardAccessId { get; set; }
        public int UserId { get; set; }
        public int BoardId { get; set; }
        public string BoardName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime AccessGrantedOn { get; set; }
    }

    public class MemberResignResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public DateTime MemberResingOn { get; set; }
    }

}
