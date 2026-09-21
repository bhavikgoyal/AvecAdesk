namespace AvecADeskApi.Model.InstituteContact
{

    public class InstituteContactDto
    {
        public int Id { get; set; }
        public int InstituteId { get; set; }
        public string? ContactName { get; set; }
        public string? Designation { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? AlternatePhone { get; set; }
        public string? Address { get; set; }
        public string? Notes { get; set; }

        //public static InstituteContactDto Empty(int instituteId) => new() { InstituteId = instituteId };
    }

    public class InstituteContactUpsertRequest
    {
        public string? ContactName { get; set; }
        public string? Designation { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? AlternatePhone { get; set; }
        public string? Address { get; set; }
        public string? Notes { get; set; }
    }
}
