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
                MapCourseData);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(CourseRepository)}.{nameof(GetCourseByIdAsync)}", ex);
            throw;
        }
    }
    public async Task<List<CourseListResponse>> GetCoursesAsync()
    {
        try
        {
            return await _db.ExecuteReaderListAsync( "sp_GetCourses",cmd => { }, MapCourseList);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(CourseRepository)}.{nameof(GetCoursesAsync)}", ex);
            throw;
        }
    }
    public async Task<int> CreateCourseAsync(CourseCreateRequest request, string? programLogoPath)
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
                cmd.Parameters.AddWithValue("@Category", (object?)request.CourseCategory  ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Fees", (object?)request.Fees ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Duration", (object?)request.Duration ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Eligibility", (object?)request.Eligibility ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Campus", (object?)request.Campus ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Level", (object?)request.Level ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ProgramLink", (object?)request.ProgramLink ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CricosCode", (object?)request.CricosCode ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Intake", (object?)request.Intake ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EnglishReq", (object?)request.EnglishReq ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ScholarshipsDetails", (object?)request.ScholarshipsDetails ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ProgramDescription", (object?)request.ProgramDescription ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@AddmissionRequirements", (object?)request.AddmissionRequirements ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ProgramLogo",(object?)programLogoPath ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IsApproved", request.IsApproved);
                cmd.Parameters.AddWithValue("@IsActive", request.IsActive);
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

    public async Task<bool> UpdateCourseAsync(int courseId, CourseUpdateRequest request, string? programLogoPath)
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
                cmd.Parameters.AddWithValue("@CourseName", request.CourseName);
                cmd.Parameters.AddWithValue("@Category", request.CourseCategory ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Fees", (object?)request.Fees ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Duration", (object?)request.Duration ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Eligibility", (object?)request.Eligibility ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Campus", (object?)request.Campus ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Level", (object?)request.Level ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ProgramLink", (object?)request.ProgramLink ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CricosCode", (object?)request.CricosCode ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Intake", (object?)request.Intake ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EnglishReq", (object?)request.EnglishReq ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ScholarshipsDetails", (object?)request.ScholarshipsDetails ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ProgramDescription", (object?)request.ProgramDescription ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@AddmissionRequirements", (object?)request.AddmissionRequirements ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ProgramLogo", (object?)programLogoPath ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IsApproved", request.IsApproved);
                cmd.Parameters.AddWithValue("@IsActive", request.IsActive);
               
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
            InstituteName = HasColumn(reader, "InstituteName") && !reader.IsDBNull(reader.GetOrdinal("InstituteName"))
                ? reader.GetString(reader.GetOrdinal("InstituteName")) : string.Empty,
            CourseName = reader.GetString(reader.GetOrdinal("CourseName")),
            Category = reader.IsDBNull(reader.GetOrdinal("Category")) ? null : reader.GetString(reader.GetOrdinal("Category")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
            Fees = reader.IsDBNull(reader.GetOrdinal("Fees")) ? null : reader.GetDecimal(reader.GetOrdinal("Fees")),
            Duration = reader.IsDBNull(reader.GetOrdinal("Duration")) ? null : reader.GetString(reader.GetOrdinal("Duration")),
            Eligibility = reader.IsDBNull(reader.GetOrdinal("Eligibility")) ? null : reader.GetString(reader.GetOrdinal("Eligibility")),
            IsAIFetched = reader.GetBoolean(reader.GetOrdinal("IsAIFetched")),
            IsApproved = reader.GetBoolean(reader.GetOrdinal("IsApproved")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            Campus = HasColumn(reader, "Campus") && !reader.IsDBNull(reader.GetOrdinal("Campus"))
                ? reader.GetString(reader.GetOrdinal("Campus")) : null,
            Level = HasColumn(reader, "Level") && !reader.IsDBNull(reader.GetOrdinal("Level"))
                ? reader.GetString(reader.GetOrdinal("Level")) : null,
            ProgramLink = HasColumn(reader, "ProgramLink") && !reader.IsDBNull(reader.GetOrdinal("ProgramLink"))
                ? reader.GetString(reader.GetOrdinal("ProgramLink")) : null,
            CricosCode = HasColumn(reader, "CricosCode") && !reader.IsDBNull(reader.GetOrdinal("CricosCode"))
                ? reader.GetString(reader.GetOrdinal("CricosCode")) : null,
            Intake = HasColumn(reader, "Intake") && !reader.IsDBNull(reader.GetOrdinal("Intake"))
                ? reader.GetString(reader.GetOrdinal("Intake")) : null,
            EnglishReq = HasColumn(reader, "EnglishReq") && !reader.IsDBNull(reader.GetOrdinal("EnglishReq"))
                ? reader.GetString(reader.GetOrdinal("EnglishReq")) : null,
            ProgramDescription = HasColumn(reader, "ProgramDescription") && !reader.IsDBNull(reader.GetOrdinal("ProgramDescription"))
                ? reader.GetString(reader.GetOrdinal("ProgramDescription")) : null,
            ProgramLogo = HasColumn(reader, "ProgramLogo") && !reader.IsDBNull(reader.GetOrdinal("ProgramLogo"))
                ? reader.GetString(reader.GetOrdinal("ProgramLogo")) : null,
            RateType = HasColumn(reader, "RateType") && !reader.IsDBNull(reader.GetOrdinal("RateType"))
                ? reader.GetString(reader.GetOrdinal("RateType")) : string.Empty,
            CommissionRate = HasColumn(reader, "CommissionRate") && !reader.IsDBNull(reader.GetOrdinal("CommissionRate"))
                ? reader.GetDecimal(reader.GetOrdinal("CommissionRate")) : 0
        };
    }

    private static bool HasColumn(SqlDataReader reader, string column)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(reader.GetName(i), column, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
    private static CourseResponse MapCourseData(SqlDataReader reader)
    {
        return new CourseResponse
        {
            CourseId = reader.GetInt32(reader.GetOrdinal("CourseId")),
            InstituteId = reader.GetInt32(reader.GetOrdinal("InstituteId")),
            InstituteName = reader.IsDBNull(reader.GetOrdinal("InstituteName"))
                ? string.Empty : reader.GetString(reader.GetOrdinal("InstituteName")),
            CourseName = reader.IsDBNull(reader.GetOrdinal("CourseName"))
                ? string.Empty : reader.GetString(reader.GetOrdinal("CourseName")),
            Category = reader.IsDBNull(reader.GetOrdinal("Category"))
                ? null : reader.GetString(reader.GetOrdinal("Category")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                ? null : reader.GetString(reader.GetOrdinal("Description")),
            Fees = reader.IsDBNull(reader.GetOrdinal("Fees"))
                ? null : reader.GetDecimal(reader.GetOrdinal("Fees")),
            Duration = reader.IsDBNull(reader.GetOrdinal("Duration"))
                ? null : reader.GetString(reader.GetOrdinal("Duration")),
            Eligibility = reader.IsDBNull(reader.GetOrdinal("Eligibility"))
                ? null : reader.GetString(reader.GetOrdinal("Eligibility")),
            IsAIFetched = reader.IsDBNull(reader.GetOrdinal("IsAIFetched"))
                ? false : reader.GetBoolean(reader.GetOrdinal("IsAIFetched")),
            IsApproved = reader.IsDBNull(reader.GetOrdinal("IsApproved"))
                ? false : reader.GetBoolean(reader.GetOrdinal("IsApproved")),
            IsActive = reader.IsDBNull(reader.GetOrdinal("IsActive"))
                ? false : reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            Campus = reader.IsDBNull(reader.GetOrdinal("Campus"))
                ? null : reader.GetString(reader.GetOrdinal("Campus")),
            Level = reader.IsDBNull(reader.GetOrdinal("Level"))
                ? null : reader.GetString(reader.GetOrdinal("Level")),
            ProgramLink = reader.IsDBNull(reader.GetOrdinal("ProgramLink"))
                ? null : reader.GetString(reader.GetOrdinal("ProgramLink")),
            CricosCode = reader.IsDBNull(reader.GetOrdinal("CricosCode"))
                ? null : reader.GetString(reader.GetOrdinal("CricosCode")),
            Intake = reader.IsDBNull(reader.GetOrdinal("Intake"))
                ? null : reader.GetString(reader.GetOrdinal("Intake")),
            EnglishReq = reader.IsDBNull(reader.GetOrdinal("EnglishReq"))
                ? null : reader.GetString(reader.GetOrdinal("EnglishReq")),
            ScholarshipsDetails = reader.IsDBNull(reader.GetOrdinal("ScholarshipsDetails"))
                ? null : reader.GetString(reader.GetOrdinal("ScholarshipsDetails")),
            ProgramDescription = reader.IsDBNull(reader.GetOrdinal("ProgramDescription"))
                ? null : reader.GetString(reader.GetOrdinal("ProgramDescription")),
            AddmissionRequirements = reader.IsDBNull(reader.GetOrdinal("AddmissionRequirements"))
                ? null : reader.GetString(reader.GetOrdinal("AddmissionRequirements")),
            ProgramLogo = reader.IsDBNull(reader.GetOrdinal("ProgramLogo"))
                ? null : reader.GetString(reader.GetOrdinal("ProgramLogo"))
          
        };
    }
    private static CourseListResponse MapCourseList(SqlDataReader reader)
    {
        return new CourseListResponse
        {
            CourseId = reader.GetInt32(reader.GetOrdinal("CourseId")),
            InstituteId = reader.GetInt32(reader.GetOrdinal("InstituteId")),
            InstituteName = reader.IsDBNull(reader.GetOrdinal("InstituteName"))
                ? string.Empty
                : reader.GetString(reader.GetOrdinal("InstituteName")),
            CourseName = reader.GetString(reader.GetOrdinal("CourseName")),
            ProgramLogo = reader.IsDBNull(reader.GetOrdinal("ProgramLogo"))
            ? null
            : reader.GetString(reader.GetOrdinal("ProgramLogo")),
            ProgramLink = reader.IsDBNull(reader.GetOrdinal("ProgramLink"))
            ? null
            : reader.GetString(reader.GetOrdinal("ProgramLink")),
            CourseCategory = reader.IsDBNull(reader.GetOrdinal("CourseCategory"))
            ? null
            : reader.GetString(reader.GetOrdinal("CourseCategory")),
            Level = reader.IsDBNull(reader.GetOrdinal("Level"))
                ? null
                : reader.GetString(reader.GetOrdinal("Level")),
            Campus = reader.IsDBNull(reader.GetOrdinal("Campus"))
                ? null
                : reader.GetString(reader.GetOrdinal("Campus")),
            Intake = reader.IsDBNull(reader.GetOrdinal("Intake"))
                ? null
                : reader.GetString(reader.GetOrdinal("Intake")),
            Fees = reader.IsDBNull(reader.GetOrdinal("Fees"))
                ? null
                : reader.GetDecimal(reader.GetOrdinal("Fees")),
            Duration = reader.IsDBNull(reader.GetOrdinal("Duration"))
                ? null
                : reader.GetString(reader.GetOrdinal("Duration")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
        };
    }
}
