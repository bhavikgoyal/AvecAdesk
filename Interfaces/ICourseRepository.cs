using AvecADeskApi.Model.Course;

namespace AvecADeskApi.Interfaces;

public interface ICourseRepository
{
    Task<List<CourseResponse>> GetCoursesByInstituteAsync(int? instituteId);
    Task<CourseResponse?> GetCourseByIdAsync(int courseId);
    Task<List<CourseListResponse>> GetCoursesAsync();
    Task<int> CreateCourseAsync(CourseCreateRequest request);
    Task<bool> UpdateCourseAsync(int courseId, CourseUpdateRequest request);
    Task<CourseResponse?> ApproveCourseAsync(int courseId, int? approvedByUserId);
    Task<bool> DeleteCourseAsync(int courseId);
}
