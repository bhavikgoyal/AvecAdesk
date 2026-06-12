using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Aih;
using AvecADeskApi.Model.Student;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AvecADeskApi.Repositories.Aih;

public class AihRepository : IAihRepository
{
    private readonly SqlDbHelper _db;
    private readonly LogHelper _logHelper;

    public AihRepository(SqlDbHelper db, LogHelper logHelper)
    {
        _db = db;
        _logHelper = logHelper;
    }

    public async Task<int> SubmitAihFormAsync(AihSubmitRequest request)
    {
        try
        {
            var interestIdParam = new SqlParameter("@InterestId", SqlDbType.Int) { Direction = ParameterDirection.Output };

            await _db.ExecuteNonQueryAsync("sp_SubmitAihForm", cmd =>
            {
                cmd.Parameters.AddWithValue("@Phone", request.Phone);
                cmd.Parameters.AddWithValue("@Name", request.Name);
                cmd.Parameters.AddWithValue("@Email", request.Email);
                cmd.Parameters.AddWithValue("@InstituteId", request.InstituteId);
                cmd.Parameters.AddWithValue("@CourseId", (object?)request.CourseId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Notes", (object?)request.Notes ?? DBNull.Value);
                cmd.Parameters.Add(interestIdParam);
            });

            return (int)interestIdParam.Value;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(AihRepository)}.{nameof(SubmitAihFormAsync)}", ex);
            throw;
        }
    }

    public async Task<List<AihResponse>> GetAihInterestsAsync(int? vendorId, int? instituteId)
    {
        try
        {
            return await _db.ExecuteReaderListAsync(
                "sp_GetAihInterests",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@VendorId", (object?)vendorId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@InstituteId", (object?)instituteId ?? DBNull.Value);
                },
                MapAih);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(AihRepository)}.{nameof(GetAihInterestsAsync)}", ex);
            throw;
        }
    }

    public async Task<AihConvertResponse?> ConvertAihToStudentAsync(int interestId)
    {
        try
        {
            return await _db.ExecuteReaderCustomAsync(
                "sp_ConvertAihToStudent",
                cmd => cmd.Parameters.AddWithValue("@InterestId", interestId),
                async reader =>
                {
                    AihResponse? interest = null;
                    StudentResponse? student = null;

                    if (await reader.ReadAsync())
                        interest = MapAih(reader);

                    if (await reader.NextResultAsync() && await reader.ReadAsync())
                        student = MapStudent(reader);

                    if (interest == null || student == null)
                        return null;

                    return new AihConvertResponse { Interest = interest, Student = student };
                });
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(AihRepository)}.{nameof(ConvertAihToStudentAsync)}", ex);
            throw;
        }
    }

    private static AihResponse MapAih(SqlDataReader reader)
    {
        return new AihResponse
        {
            InterestId = reader.GetInt32(reader.GetOrdinal("InterestId")),
            Phone = reader.GetString(reader.GetOrdinal("Phone")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            InstituteId = reader.GetInt32(reader.GetOrdinal("InstituteId")),
            CourseId = reader.IsDBNull(reader.GetOrdinal("CourseId")) ? null : reader.GetInt32(reader.GetOrdinal("CourseId")),
            Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes")),
            IsConvertedToLead = reader.GetBoolean(reader.GetOrdinal("IsConvertedToLead")),
            IsConvertedToStudent = reader.GetBoolean(reader.GetOrdinal("IsConvertedToStudent")),
            StudentId = reader.IsDBNull(reader.GetOrdinal("StudentId")) ? null : reader.GetInt32(reader.GetOrdinal("StudentId")),
            SubmittedAt = reader.GetDateTime(reader.GetOrdinal("SubmittedAt"))
        };
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
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }
}
