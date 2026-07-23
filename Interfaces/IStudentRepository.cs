using AvecADeskApi.Model.Student;

namespace AvecADeskApi.Interfaces;

public interface IStudentRepository
{
    Task<List<AllStudentResponse>> GetAllStudentsAsync();
    Task<List<StudentResponse>> GetStudentsAsync(string? enrolmentStatus);
    Task<StudentResponse?> GetStudentByIdAsync(int studentId);
    Task<int> CreateStudentAsync(StudentCreateRequest request, DateTime? aihFormSubmittedAt = null);
    Task<bool> UpdateStudentAsync(int studentId, StudentUpdateRequest request);
    Task<bool> UpdateStudentEnrolmentStatusAsync(int studentId, string enrolmentStatus);
}
