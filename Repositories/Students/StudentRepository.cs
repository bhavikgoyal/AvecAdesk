using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.PaymentSchedule;
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
                cmd.Parameters.AddWithValue("@ScrappingId", request.InstituteId);
                cmd.Parameters.AddWithValue("@CourseId", (object?)request.CourseId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FullName", request.FullName);
                cmd.Parameters.AddWithValue("@Email", request.Email);
                cmd.Parameters.AddWithValue("@Phone", request.Phone);
                cmd.Parameters.AddWithValue("@EnrollmentNumber", (object?)request.EnrollmentNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EnrolmentStatus", request.EnrolmentStatus);
                cmd.Parameters.AddWithValue("@AIHFormSubmittedAt", (object?)aihFormSubmittedAt ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FolderNo", (object?)request.FolderNo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CourseStartDate",(object?)request.CourseStartDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CourseEndDate",(object?)request.CourseEndDate ?? DBNull.Value);
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
    public async Task<StudentPaymentScheduleDetailResponse?> GetStudentPaymentDetailByIdAsync(int studentId)
    {
        try
        {
            return await _db.ExecuteReaderCustomAsync(
                "sp_GetStudentPaymentDetailById",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@StudentId", studentId);
                },
                async reader =>
                {
                    if (!await reader.ReadAsync())
                        return null;

                    var response = MapStudentPaymentDetail(reader);

                    if (await reader.NextResultAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            response.StudentPaymentList.Add(MapStudentPaymentItem(reader));
                        }
                    }

                    if (await reader.NextResultAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            response.CommissionHistory.Add(MapCommissionHistoryItem(reader));
                        }
                    }

                    return response;
                });
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(StudentRepository)}.{nameof(GetStudentPaymentDetailByIdAsync)}", ex);
            throw;
        }
    }
    private static StudentPaymentScheduleDetailResponse MapStudentPaymentDetail(SqlDataReader reader)
    {
        return new StudentPaymentScheduleDetailResponse
        {
            StudentId = reader.GetInt32(reader.GetOrdinal("StudentId")),
            InstituteId = reader.GetInt32(reader.GetOrdinal("InstituteId")),
            InstituteName = reader["InstituteName"]?.ToString(),

            CourseId = reader.IsDBNull(reader.GetOrdinal("CourseId"))
                ? null
                : reader.GetInt32(reader.GetOrdinal("CourseId")),

            CourseName = reader["CourseName"]?.ToString(),
            Campus = reader["Campus"]?.ToString(),

            FullName = reader["FullName"]?.ToString(),
            Email = reader["Email"]?.ToString(),
            Phone = reader["Phone"]?.ToString(),
            FolderNo = reader["FolderNo"]?.ToString(),

            CourseStartDate = reader.IsDBNull(reader.GetOrdinal("CourseStartDate"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("CourseStartDate")),

            CourseEndDate = reader.IsDBNull(reader.GetOrdinal("CourseEndDate"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("CourseEndDate")),

            ScheduleId = reader.GetInt32(reader.GetOrdinal("ScheduleId")),
            FirstDueDate = reader.GetDateTime(reader.GetOrdinal("FirstDueDate")),
            TotalCourseFee = reader.GetDecimal(reader.GetOrdinal("TotalCourseFee")),
            NoOfInstallments = reader.GetInt32(reader.GetOrdinal("NoOfInstallments")),
            Frequency = reader["Frequency"]?.ToString(),

            CommissionId = reader.IsDBNull(reader.GetOrdinal("CommissionId"))
                ? null
                : reader.GetInt32(reader.GetOrdinal("CommissionId")),

            CommissionPercentage = reader.IsDBNull(reader.GetOrdinal("CommissionPercentage"))
                ? null
                : reader.GetDecimal(reader.GetOrdinal("CommissionPercentage")),

            GSTPercentage = reader.IsDBNull(reader.GetOrdinal("GSTPercentage"))
                ? null
                : reader.GetDecimal(reader.GetOrdinal("GSTPercentage")),

            BonusType = reader["BonusType"]?.ToString(),
            BonusOption = reader["BonusOption"]?.ToString(),

            DueDate = reader.IsDBNull(reader.GetOrdinal("DueDate"))
            ? null
            : reader.GetDateTime(reader.GetOrdinal("DueDate")),

            CommissionAmount = reader.IsDBNull(reader.GetOrdinal("CommissionAmount"))
            ? null
            : reader.GetDecimal(reader.GetOrdinal("CommissionAmount")),

            GSTAmount = reader.IsDBNull(reader.GetOrdinal("GSTAmount"))
            ? null
            : reader.GetDecimal(reader.GetOrdinal("GSTAmount")),

            BonusAmount = reader.IsDBNull(reader.GetOrdinal("BonusAmount"))
            ? null
            : reader.GetDecimal(reader.GetOrdinal("BonusAmount")),
            Remark = reader["Remark"]?.ToString(),
        };
    }
    private static StudentPaymentItem MapStudentPaymentItem(SqlDataReader reader)
    {
        return new StudentPaymentItem
        {
            StudentPaymentInstallmentId = reader.GetInt32(reader.GetOrdinal("StudentPaymentInstallmentId")),
            ScheduleId = reader.GetInt32(reader.GetOrdinal("ScheduleId")),
            InstallmentNo = reader.GetInt32(reader.GetOrdinal("InstallmentNo")),
            DueDate = reader.GetDateTime(reader.GetOrdinal("DueDate")),
            FeesAmount = reader.GetDecimal(reader.GetOrdinal("FeesAmount")),
            PaidAmount = reader.GetDecimal(reader.GetOrdinal("PaidAmount")),
            BalanceAmount = reader.GetDecimal(reader.GetOrdinal("BalanceAmount")),
            PaymentStatus = reader["PaymentStatus"]?.ToString(),
            PaidDate = reader.IsDBNull(reader.GetOrdinal("PaidDate"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("PaidDate"))
        };
    }
    private static CommissionHistoryItem MapCommissionHistoryItem(SqlDataReader reader)
    {
        return new CommissionHistoryItem
        {
            CommissionDetailId = reader.GetInt32(reader.GetOrdinal("CommissionDetailId")),
            InstallmentNo = reader.GetInt32(reader.GetOrdinal("InstallmentNo")),
            DueDate = reader.GetDateTime(reader.GetOrdinal("DueDate")),
            FeesAmount = reader.GetDecimal(reader.GetOrdinal("FeesAmount")),
            PaymentStatus = reader["PaymentStatus"]?.ToString(),

            CommissionAmount = reader.GetDecimal(reader.GetOrdinal("CommissionAmount")),
            GSTAmount = reader.GetDecimal(reader.GetOrdinal("GSTAmount")),
            BonusAmount = reader.GetDecimal(reader.GetOrdinal("BonusAmount")),
            InvoiceAmount = reader.GetDecimal(reader.GetOrdinal("InvoiceAmount")),

            InvoiceNo = reader["InvoiceNo"]?.ToString(),

            ReceivedDate = reader.IsDBNull(reader.GetOrdinal("ReceivedDate"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("ReceivedDate")),

            CommissionStatus = reader["CommissionStatus"]?.ToString(),
        };
    }

}
