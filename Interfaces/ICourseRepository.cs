using AvecADeskApi.Model.Course;

namespace AvecADeskApi.Interfaces;

public interface ICourseRepository
{
    Task<List<CourseResponse>> GetCoursesByInstituteAsync(int? instituteId);
    Task<CourseResponse?> GetCourseByIdAsync(int courseId);
    Task<List<CourseListResponse>> GetCoursesAsync();
    Task<int> CreateCourseAsync(CourseCreateRequest request, string? programLogoPath);
    Task<bool> UpdateCourseAsync(int courseId, CourseUpdateRequest request, string? programLogoPath);
    Task<CourseResponse?> ApproveCourseAsync(int courseId, int? approvedByUserId);
    Task<bool> DeleteCourseAsync(int courseId);
}
