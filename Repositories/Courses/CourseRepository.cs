using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Course;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AvecADeskApi.Repositories.Courses;

public class CourseRepository : ICourseRepository
{
    private readonly SqlDbHelper _db;
    private readonly LogHelper _logHelper;

    public CourseRepository(SqlDbHelper db, LogHelper logHelper)
    {
        _db = db;
        _logHelper = logHelper;
    }

    public async Task<List<CourseResponse>> GetCoursesByInstituteAsync(int? instituteId)
    {
        try
        {
            return await _db.ExecuteReaderListAsync(
                "sp_GetCoursesByInstitute",
                cmd => cmd.Parameters.AddWithValue("@InstituteId", (object?)instituteId ?? DBNull.Value),
                MapCourse);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(CourseRepository)}.{nameof(GetCoursesByInstituteAsync)}", ex);
            throw;
        }
    }

    public async Task<CourseResponse?> GetCourseByIdAsync(int courseId)
    {
        try
        {
            return await _db.ExecuteReaderSingleAsync(
                "sp_GetCourseById",
                cmd => cmd.Parameters.AddWithValue("@CourseId", courseId),
                MapCourse);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(CourseRepository)}.{nameof(GetCourseByIdAsync)}", ex);
            throw;
        }
    }

    public async Task<int> CreateCourseAsync(CourseCreateRequest request)
    {
        try
        {
            var courseIdParam = new SqlParameter("@CourseId", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            await _db.ExecuteNonQueryAsync("sp_CreateCourse", cmd =>
            {
                cmd.Parameters.AddWithValue("@InstituteId", request.InstituteId);
                cmd.Parameters.AddWithValue("@CourseName", request.CourseName);
                cmd.Parameters.AddWithValue("@Category", (object?)request.Category ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Fees", (object?)request.Fees ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Duration", (object?)request.Duration ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Eligibility", (object?)request.Eligibility ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IsAIFetched", request.IsAIFetched);
                cmd.Parameters.Add(courseIdParam);
            });

            return (int)courseIdParam.Value;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(CourseRepository)}.{nameof(CreateCourseAsync)}", ex);
            throw;
        }
    }

    public async Task<bool> UpdateCourseAsync(int courseId, CourseUpdateRequest request)
    {
        try
        {
            var rowsAffectedParam = new SqlParameter("@RowsAffected", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            await _db.ExecuteNonQueryAsync("sp_UpdateCourse", cmd =>
            {
                cmd.Parameters.AddWithValue("@CourseId", courseId);
                cmd.Parameters.AddWithValue("@InstituteId", request.InstituteId);
                cmd.Parameters.AddWithValue("@CourseName", request.CourseName);
                cmd.Parameters.AddWithValue("@Category", (object?)request.Category ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Fees", (object?)request.Fees ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Duration", (object?)request.Duration ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Eligibility", (object?)request.Eligibility ?? DBNull.Value);
                cmd.Parameters.Add(rowsAffectedParam);
            });

            return (int)rowsAffectedParam.Value > 0;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(CourseRepository)}.{nameof(UpdateCourseAsync)}", ex);
            throw;
        }
    }

    public async Task<CourseResponse?> ApproveCourseAsync(int courseId, int? approvedByUserId)
    {
        try
        {
            return await _db.ExecuteReaderSingleAsync(
                "sp_ApproveCourse",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@CourseId", courseId);
                    cmd.Parameters.AddWithValue("@ApprovedByUserId", (object?)approvedByUserId ?? DBNull.Value);
                },
                MapCourse);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(CourseRepository)}.{nameof(ApproveCourseAsync)}", ex);
            throw;
        }
    }

    public async Task<bool> DeleteCourseAsync(int courseId)
    {
        try
        {
            var rowsAffectedParam = new SqlParameter("@RowsAffected", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            await _db.ExecuteNonQueryAsync("sp_DeleteCourse", cmd =>
            {
                cmd.Parameters.AddWithValue("@CourseId", courseId);
                cmd.Parameters.Add(rowsAffectedParam);
            });

            return (int)rowsAffectedParam.Value > 0;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(CourseRepository)}.{nameof(DeleteCourseAsync)}", ex);
            throw;
        }
    }

    private static CourseResponse MapCourse(SqlDataReader reader)
    {
        return new CourseResponse
        {
            CourseId = reader.GetInt32(reader.GetOrdinal("CourseId")),
            InstituteId = reader.GetInt32(reader.GetOrdinal("InstituteId")),
            CourseName = reader.GetString(reader.GetOrdinal("CourseName")),
            Category = reader.IsDBNull(reader.GetOrdinal("Category")) ? null : reader.GetString(reader.GetOrdinal("Category")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
            Fees = reader.IsDBNull(reader.GetOrdinal("Fees")) ? null : reader.GetDecimal(reader.GetOrdinal("Fees")),
            Duration = reader.IsDBNull(reader.GetOrdinal("Duration")) ? null : reader.GetString(reader.GetOrdinal("Duration")),
            Eligibility = reader.IsDBNull(reader.GetOrdinal("Eligibility")) ? null : reader.GetString(reader.GetOrdinal("Eligibility")),
            IsAIFetched = reader.GetBoolean(reader.GetOrdinal("IsAIFetched")),
            IsApproved = reader.GetBoolean(reader.GetOrdinal("IsApproved")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }
}
