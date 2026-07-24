using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Student;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AvecADeskApi.Repositories.Students;

public class StudentRepository : IStudentRepository
{
    private readonly SqlDbHelper _db;
    private readonly LogHelper _logHelper;

    public StudentRepository(SqlDbHelper db, LogHelper logHelper)
    {
        _db = db;
        _logHelper = logHelper;
    }

    public async Task<List<StudentResponse>> GetStudentsAsync(string? enrolmentStatus)
    {
        try
        {
            return await _db.ExecuteReaderListAsync(
                "sp_GetStudents",
                cmd => cmd.Parameters.AddWithValue("@EnrolmentStatus", (object?)enrolmentStatus ?? DBNull.Value),
                MapStudent);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(StudentRepository)}.{nameof(GetStudentsAsync)}", ex);
            throw;
        }
    }

    public async Task<List<AllStudentResponse>> GetAllStudentsAsync()
    {
        try
        {
            return await _db.ExecuteReaderListAsync(
                "sp_GetAllStudents",
                _ => { },
                MapAllStudent);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(StudentRepository)}.{nameof(GetAllStudentsAsync)}", ex);
            throw;
        }
    }

    public async Task<StudentResponse?> GetStudentByIdAsync(int studentId)
    {
        try
        {
            return await _db.ExecuteReaderSingleAsync(
                "sp_GetStudentById",
                cmd => cmd.Parameters.AddWithValue("@StudentId", studentId),
                MapStudent);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(StudentRepository)}.{nameof(GetStudentByIdAsync)}", ex);
            throw;
        }
    }

    public async Task<int> CreateStudentAsync(StudentCreateRequest request, DateTime? aihFormSubmittedAt = null)
    {
        try
        {
            var studentIdParam = new SqlParameter("@StudentId", SqlDbType.Int) { Direction = ParameterDirection.Output };

            await _db.ExecuteNonQueryAsync("sp_CreateStudent", cmd =>
            {
                cmd.Parameters.AddWithValue("@InstituteId", request.InstituteId);
                cmd.Parameters.AddWithValue("@CourseId", (object?)request.CourseId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FullName", request.FullName);
                cmd.Parameters.AddWithValue("@Email", request.Email);
                cmd.Parameters.AddWithValue("@Phone", request.Phone);
                cmd.Parameters.AddWithValue("@EnrollmentNumber", (object?)request.EnrollmentNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EnrolmentStatus", request.EnrolmentStatus);
                cmd.Parameters.AddWithValue("@AIHFormSubmittedAt", (object?)aihFormSubmittedAt ?? DBNull.Value);
                cmd.Parameters.Add(studentIdParam);
            });

            return (int)studentIdParam.Value;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(StudentRepository)}.{nameof(CreateStudentAsync)}", ex);
            throw;
        }
    }

    public async Task<bool> UpdateStudentAsync(int studentId, StudentUpdateRequest request)
    {
        try
        {
            var rowsAffectedParam = new SqlParameter("@RowsAffected", SqlDbType.Int) { Direction = ParameterDirection.Output };

            await _db.ExecuteNonQueryAsync("sp_UpdateStudent", cmd =>
            {
                cmd.Parameters.AddWithValue("@StudentId", studentId);
                cmd.Parameters.AddWithValue("@InstituteId", request.InstituteId);
                cmd.Parameters.AddWithValue("@CourseId", (object?)request.CourseId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FullName", request.FullName);
                cmd.Parameters.AddWithValue("@Email", request.Email);
                cmd.Parameters.AddWithValue("@Phone", request.Phone);
                cmd.Parameters.AddWithValue("@EnrollmentNumber", (object?)request.EnrollmentNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IsActive", request.IsActive);
                cmd.Parameters.Add(rowsAffectedParam);
            });

            return (int)rowsAffectedParam.Value > 0;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(StudentRepository)}.{nameof(UpdateStudentAsync)}", ex);
            throw;
        }
    }

    public async Task<bool> UpdateStudentEnrolmentStatusAsync(int studentId, string enrolmentStatus)
    {
        try
        {
            var rowsAffectedParam = new SqlParameter("@RowsAffected", SqlDbType.Int) { Direction = ParameterDirection.Output };

            await _db.ExecuteNonQueryAsync("sp_UpdateStudentEnrolmentStatus", cmd =>
            {
                cmd.Parameters.AddWithValue("@StudentId", studentId);
                cmd.Parameters.AddWithValue("@EnrolmentStatus", enrolmentStatus);
                cmd.Parameters.Add(rowsAffectedParam);
            });

            return (int)rowsAffectedParam.Value > 0;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(StudentRepository)}.{nameof(UpdateStudentEnrolmentStatusAsync)}", ex);
            throw;
        }
    }

    private static StudentResponse MapStudent(SqlDataReader reader)
    {
        return new StudentResponse
        {
            StudentId = reader.GetInt32(reader.GetOrdinal("StudentId")),
            InstituteId = reader.GetInt32(reader.GetOrdinal("InstituteId")),
            CourseId = reader.IsDBNull(reader.GetOrdinal("CourseId")) ? null : reader.GetInt32(reader.GetOrdinal("CourseId")),
            FullName = reader.GetString(reader.GetOrdinal("FullName")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            Phone = reader.GetString(reader.GetOrdinal("Phone")),
            EnrollmentNumber = reader.IsDBNull(reader.GetOrdinal("EnrollmentNumber")) ? null : reader.GetString(reader.GetOrdinal("EnrollmentNumber")),
            EnrolmentStatus = reader.GetString(reader.GetOrdinal("EnrolmentStatus")),
            AIHFormSubmittedAt = reader.IsDBNull(reader.GetOrdinal("AIHFormSubmittedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("AIHFormSubmittedAt")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
          

        };
    }

    private static AllStudentResponse MapAllStudent(SqlDataReader reader)
    {
        return new AllStudentResponse
        {
            StudentId = reader.GetInt32(reader.GetOrdinal("StudentId")),
            ScrappingId = reader.IsDBNull(reader.GetOrdinal("ScrappingId")) ? null : reader.GetInt32(reader.GetOrdinal("ScrappingId")),
            CourseId = reader.IsDBNull(reader.GetOrdinal("CourseId")) ? null : reader.GetInt32(reader.GetOrdinal("CourseId")),
            FullName = reader.GetString(reader.GetOrdinal("FullName")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            Phone = reader.GetString(reader.GetOrdinal("Phone")),
            EnrollmentNumber = reader.IsDBNull(reader.GetOrdinal("EnrollmentNumber")) ? null : reader.GetString(reader.GetOrdinal("EnrollmentNumber")),
            EnrolmentStatus = reader.GetString(reader.GetOrdinal("EnrolmentStatus")),
            AIHFormSubmittedAt = reader.IsDBNull(reader.GetOrdinal("AIHFormSubmittedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("AIHFormSubmittedAt")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }
}
