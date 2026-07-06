namespace AvecADeskApi.Model.Student
{
    public class RegisterStudentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string VerificationCode { get; set; } = string.Empty;
    }
}
