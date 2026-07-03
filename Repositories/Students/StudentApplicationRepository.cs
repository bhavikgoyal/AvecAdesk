using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Student;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AvecADeskApi.Repositories.Students;

public class StudentApplicationRepository : IStudentApplicationRepository
{
    private readonly SqlDbHelper _db;
    private readonly LogHelper _logHelper;

    public StudentApplicationRepository(SqlDbHelper db, LogHelper logHelper)
    {
        _db = db;
        _logHelper = logHelper;
    }
    public async Task<List<StudentApplicationDetailsModel>> GetStudentApplicationsAsync(string? search, int pagenumber, int pageSize)
    {
        try
        {
            return await _db.ExecuteReaderListAsync(
                "sp_GetStudentApplicationDetails",
                cmd => 
                {
                    cmd.Parameters.AddWithValue("@Search", search ?? string.Empty);
                    cmd.Parameters.AddWithValue("@PageNumber", pagenumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                },
                MapStudentApplicationDetail);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(StudentApplicationRepository)}.{nameof(GetStudentApplicationsAsync)}", ex);
            throw;
        }
    }
    public async Task<StudentApplicationResponse?> GetApplicationByIdAsync(Guid applicationId)
    {
        try
        {
            var application = await _db.ExecuteReaderSingleAsync(
                "sp_GetStudentApplicationById",
                cmd => cmd.Parameters.AddWithValue("@ApplicationId", applicationId),
                MapApplication);

            if (application == null) return null;

            application.Detail = await _db.ExecuteReaderSingleAsync(
                "sp_GetApplicationDetailByApplicationId",
                cmd => cmd.Parameters.AddWithValue("@ApplicationId", applicationId),
                MapDetail);

            application.Declaration = await _db.ExecuteReaderSingleAsync(
                "sp_GetDeclarationByApplicationId",
                cmd => cmd.Parameters.AddWithValue("@ApplicationId", applicationId),
                MapDeclaration);

            application.Documents = await _db.ExecuteReaderListAsync(
                "sp_GetDocumentsByApplicationId",
                cmd => cmd.Parameters.AddWithValue("@ApplicationId", applicationId),
                MapDocument);

            return application;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(StudentApplicationRepository)}.{nameof(GetApplicationByIdAsync)}", ex);
            throw;
        }
    }

    public async Task<Guid> CreateApplicationAsync(StudentApplicationCreateRequest request)
    {
        try
        {
            var applicationIdParam = new SqlParameter("@ApplicationId", SqlDbType.UniqueIdentifier)
            {
                Direction = ParameterDirection.Output
            };

            await _db.ExecuteNonQueryAsync("sp_CreateStudentApplication", cmd =>
            {
                cmd.Parameters.AddWithValue("@CountryApplyingFor", request.CountryApplyingFor);
                cmd.Parameters.Add(applicationIdParam);
            });

            return (Guid)applicationIdParam.Value;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(StudentApplicationRepository)}.{nameof(CreateApplicationAsync)}", ex);
            throw;
        }
    }

    //public async Task<bool> SaveApplicationDetailAsync(Guid applicationId, ApplicationDetailRequest request)
    //{
    //    try
    //    {
    //        var rowsAffectedParam = new SqlParameter("@RowsAffected", SqlDbType.Int)
    //        {
    //            Direction = ParameterDirection.Output
    //        };

    //        await _db.ExecuteNonQueryAsync("sp_SaveApplicationDetail", cmd =>
    //        {
    //            cmd.Parameters.AddWithValue("@ApplicationId", applicationId);
    //            cmd.Parameters.AddWithValue("@FirstName", (object?)request.FirstName ?? DBNull.Value);
    //            cmd.Parameters.AddWithValue("@LastName", (object?)request.LastName ?? DBNull.Value);
    //            cmd.Parameters.AddWithValue("@Email", (object?)request.Email ?? DBNull.Value);
    //            cmd.Parameters.AddWithValue("@Phone", (object?)request.Phone ?? DBNull.Value);
    //            cmd.Parameters.AddWithValue("@DateOfBirth", (object?)request.DateOfBirth ?? DBNull.Value);
    //            cmd.Parameters.AddWithValue("@Nationality", (object?)request.Nationality ?? DBNull.Value);
    //            cmd.Parameters.AddWithValue("@PassportNumber", (object?)request.PassportNumber ?? DBNull.Value);
    //            cmd.Parameters.AddWithValue("@EmergencyContactName", (object?)request.EmergencyContactName ?? DBNull.Value);
    //            cmd.Parameters.AddWithValue("@EmergencyContactPhone", (object?)request.EmergencyContactPhone ?? DBNull.Value);
    //            cmd.Parameters.AddWithValue("@EmergencyContactRelation", (object?)request.EmergencyContactRelation ?? DBNull.Value);
    //            cmd.Parameters.AddWithValue("@AppliedVisaBefore", request.AppliedVisaBefore);
    //            cmd.Parameters.AddWithValue("@PreviousVisaType", request.AppliedVisaBefore ? (object?)request.PreviousVisaType ?? DBNull.Value : DBNull.Value);
    //            cmd.Parameters.AddWithValue("@RefusedVisa", request.RefusedVisa);
    //            cmd.Parameters.AddWithValue("@RefusedCountry", request.RefusedVisa ? (object?)request.RefusedCountry ?? DBNull.Value : DBNull.Value);
    //            cmd.Parameters.AddWithValue("@RefusedVisaType", request.RefusedVisa ? (object?)request.RefusedVisaType ?? DBNull.Value : DBNull.Value);
    //            cmd.Parameters.AddWithValue("@EnglishTestName", (object?)request.EnglishTestName ?? DBNull.Value);
    //            cmd.Parameters.AddWithValue("@EnglishTestScore", (object?)request.EnglishTestScore ?? DBNull.Value);
    //            cmd.Parameters.AddWithValue("@EnglishTestDate", (object?)request.EnglishTestDate ?? DBNull.Value);
    //            cmd.Parameters.AddWithValue("@HighestQualification", (object?)request.HighestQualification ?? DBNull.Value);
    //            cmd.Parameters.Add(rowsAffectedParam);
    //        });

    //        return (int)rowsAffectedParam.Value > 0;
    //    }
    //    catch (Exception ex)
    //    {
    //        _logHelper.LogError($"{nameof(StudentApplicationRepository)}.{nameof(SaveApplicationDetailAsync)}", ex);
    //        throw;
    //    }
    //}


    public async Task<ApplicationDetailResponse?> SaveApplicationDetailAsync(Guid applicationId,ApplicationDetailRequest request)
    {
        try
        {
            var rowsAffectedParam = new SqlParameter("@RowsAffected", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };
            return await _db.ExecuteReaderSingleAsync<ApplicationDetailResponse>("sp_SaveApplicationDetail",cmd =>
                {
                    cmd.Parameters.AddWithValue("@ApplicationId", applicationId);
                    cmd.Parameters.AddWithValue("@FirstName", (object?)request.FirstName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LastName", (object?)request.LastName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", (object?)request.Email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Phone", (object?)request.Phone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DateOfBirth", (object?)request.DateOfBirth ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Nationality", (object?)request.Nationality ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PassportNumber", (object?)request.PassportNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EmergencyContactName", (object?)request.EmergencyContactName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EmergencyContactPhone", (object?)request.EmergencyContactPhone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EmergencyContactRelation", (object?)request.EmergencyContactRelation ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@AppliedVisaBefore", request.AppliedVisaBefore);
                    cmd.Parameters.AddWithValue("@PreviousVisaType",request.AppliedVisaBefore ? (object?)request.PreviousVisaType ?? DBNull.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@RefusedVisa", request.RefusedVisa);
                    cmd.Parameters.AddWithValue("@RefusedCountry", request.RefusedVisa ? (object?)request.RefusedCountry ?? DBNull.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@RefusedVisaType", request.RefusedVisa ? (object?)request.RefusedVisaType ?? DBNull.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@EnglishTestName", (object?)request.EnglishTestName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EnglishTestScore", (object?)request.EnglishTestScore ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EnglishTestDate", (object?)request.EnglishTestDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@HighestQualification", (object?)request.HighestQualification ?? DBNull.Value);
                    cmd.Parameters.Add(rowsAffectedParam);
                },
                reader => new ApplicationDetailResponse
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    UserName = reader["FullName"] as string
                });
        }
        catch (Exception ex)
        {
            _logHelper.LogError(
                $"{nameof(StudentApplicationRepository)}.{nameof(SaveApplicationDetailAsync)}",
                ex);
            throw;
        }
    }
    public async Task<ApplicationDocumentResponse> UploadDocumentAsync(Guid applicationId, ApplicationDocumentRequest request, string fileUrl)
    {
        try
        {
            var documentIdParam = new SqlParameter("@DocumentId", SqlDbType.UniqueIdentifier)
            {
                Direction = ParameterDirection.Output
            };

            await _db.ExecuteNonQueryAsync("sp_UploadApplicationDocument", cmd =>
            {
                cmd.Parameters.AddWithValue("@ApplicationId", applicationId);
                cmd.Parameters.AddWithValue("@DocCategory", request.DocCategory);
                cmd.Parameters.AddWithValue("@DocType", request.DocType);
                cmd.Parameters.AddWithValue("@FileUrl", fileUrl);
                cmd.Parameters.AddWithValue("@IsMandatory", request.IsMandatory);
                cmd.Parameters.Add(documentIdParam);
            });

            return new ApplicationDocumentResponse
            {
                Id = (Guid)documentIdParam.Value,
                ApplicationId = applicationId,
                DocCategory = request.DocCategory,
                DocType = request.DocType,
                FileUrl = fileUrl,
                IsMandatory = request.IsMandatory,
                UploadedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(StudentApplicationRepository)}.{nameof(UploadDocumentAsync)}", ex);
            throw;
        }
    }

    public async Task<bool> DeleteDocumentAsync(Guid documentId)
    {
        try
        {
            var rowsAffectedParam = new SqlParameter("@RowsAffected", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            await _db.ExecuteNonQueryAsync("sp_DeleteApplicationDocument", cmd =>
            {
                cmd.Parameters.AddWithValue("@DocumentId", documentId);
                cmd.Parameters.Add(rowsAffectedParam);
            });

            return (int)rowsAffectedParam.Value > 0;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(StudentApplicationRepository)}.{nameof(DeleteDocumentAsync)}", ex);
            throw;
        }
    }

    public async Task<bool> SaveDeclarationAsync(Guid applicationId, DeclarationRequest request)
    {
        try
        {
            var rowsAffectedParam = new SqlParameter("@RowsAffected", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            await _db.ExecuteNonQueryAsync("sp_SaveDeclaration", cmd =>
            {
                cmd.Parameters.AddWithValue("@ApplicationId", applicationId);
                cmd.Parameters.AddWithValue("@ApplicantSigned", request.ApplicantSigned);
                cmd.Parameters.AddWithValue("@ApplicantSignatureDate", (object?)request.ApplicantSignatureDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ParentSigned", request.ParentSigned);
                cmd.Parameters.AddWithValue("@ParentSignatureDate", (object?)request.ParentSignatureDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ChecklistAllSectionsCompleted", request.ChecklistAllSectionsCompleted);
                cmd.Parameters.AddWithValue("@ChecklistAcademicTranscripts", request.ChecklistAcademicTranscripts);
                cmd.Parameters.AddWithValue("@ChecklistPassportCopy", request.ChecklistPassportCopy);
                cmd.Parameters.AddWithValue("@ChecklistEnglishProficiency", request.ChecklistEnglishProficiency);
                cmd.Parameters.AddWithValue("@ChecklistGSFormSubmitted", request.ChecklistGSFormSubmitted);
                cmd.Parameters.AddWithValue("@ChecklistDeclarationSigned", request.ChecklistDeclarationSigned);
                cmd.Parameters.Add(rowsAffectedParam);
            });

            return (int)rowsAffectedParam.Value > 0;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(StudentApplicationRepository)}.{nameof(SaveDeclarationAsync)}", ex);
            throw;
        }
    }

    public async Task<bool> SubmitApplicationAsync(Guid applicationId)
    {
        try
        {
            var rowsAffectedParam = new SqlParameter("@RowsAffected", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            await _db.ExecuteNonQueryAsync("sp_SubmitStudentApplication", cmd =>
            {
                cmd.Parameters.AddWithValue("@ApplicationId", applicationId);
                cmd.Parameters.Add(rowsAffectedParam);
            });

            return (int)rowsAffectedParam.Value > 0;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(StudentApplicationRepository)}.{nameof(SubmitApplicationAsync)}", ex);
            throw;
        }
    }

    
    private static StudentApplicationResponse MapApplication(SqlDataReader reader)
    {
        return new StudentApplicationResponse
        {
            Id = reader.GetGuid(reader.GetOrdinal("Id")),
            CountryApplyingFor = reader.GetString(reader.GetOrdinal("CountryApplyingFor")),
            Status = reader.GetString(reader.GetOrdinal("Status")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            SubmittedAt = reader.IsDBNull(reader.GetOrdinal("SubmittedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("SubmittedAt"))
        };
    }

    private static ApplicationDetailResponse MapDetail(SqlDataReader reader)
    {
        return new ApplicationDetailResponse
        {
            Id = reader.GetGuid(reader.GetOrdinal("Id")),
            ApplicationId = reader.GetGuid(reader.GetOrdinal("ApplicationId")),
            FirstName = reader.IsDBNull(reader.GetOrdinal("FirstName")) ? null : reader.GetString(reader.GetOrdinal("FirstName")),
            LastName = reader.IsDBNull(reader.GetOrdinal("LastName")) ? null : reader.GetString(reader.GetOrdinal("LastName")),
            Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
            Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString(reader.GetOrdinal("Phone")),
            DateOfBirth = reader.IsDBNull(reader.GetOrdinal("DateOfBirth")) ? null : reader.GetDateTime(reader.GetOrdinal("DateOfBirth")),
            Nationality = reader.IsDBNull(reader.GetOrdinal("Nationality")) ? null : reader.GetString(reader.GetOrdinal("Nationality")),
            PassportNumber = reader.IsDBNull(reader.GetOrdinal("PassportNumber")) ? null : reader.GetString(reader.GetOrdinal("PassportNumber")),
            EmergencyContactName = reader.IsDBNull(reader.GetOrdinal("EmergencyContactName")) ? null : reader.GetString(reader.GetOrdinal("EmergencyContactName")),
            EmergencyContactPhone = reader.IsDBNull(reader.GetOrdinal("EmergencyContactPhone")) ? null : reader.GetString(reader.GetOrdinal("EmergencyContactPhone")),
            EmergencyContactRelation = reader.IsDBNull(reader.GetOrdinal("EmergencyContactRelation")) ? null : reader.GetString(reader.GetOrdinal("EmergencyContactRelation")),
            AppliedVisaBefore = reader.GetBoolean(reader.GetOrdinal("AppliedVisaBefore")),
            PreviousVisaType = reader.IsDBNull(reader.GetOrdinal("PreviousVisaType")) ? null : reader.GetString(reader.GetOrdinal("PreviousVisaType")),
            RefusedVisa = reader.GetBoolean(reader.GetOrdinal("RefusedVisa")),
            RefusedCountry = reader.IsDBNull(reader.GetOrdinal("RefusedCountry")) ? null : reader.GetString(reader.GetOrdinal("RefusedCountry")),
            RefusedVisaType = reader.IsDBNull(reader.GetOrdinal("RefusedVisaType")) ? null : reader.GetString(reader.GetOrdinal("RefusedVisaType")),
            EnglishTestName = reader.IsDBNull(reader.GetOrdinal("EnglishTestName")) ? null : reader.GetString(reader.GetOrdinal("EnglishTestName")),
            EnglishTestScore = reader.IsDBNull(reader.GetOrdinal("EnglishTestScore")) ? null : reader.GetString(reader.GetOrdinal("EnglishTestScore")),
            EnglishTestDate = reader.IsDBNull(reader.GetOrdinal("EnglishTestDate")) ? null : reader.GetDateTime(reader.GetOrdinal("EnglishTestDate")),
            HighestQualification = reader.IsDBNull(reader.GetOrdinal("HighestQualification")) ? null : reader.GetString(reader.GetOrdinal("HighestQualification")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
        };
    }

    private static DeclarationResponse MapDeclaration(SqlDataReader reader)
    {
        return new DeclarationResponse
        {
            Id = reader.GetGuid(reader.GetOrdinal("Id")),
            ApplicationId = reader.GetGuid(reader.GetOrdinal("ApplicationId")),
            ApplicantSigned = reader.GetBoolean(reader.GetOrdinal("ApplicantSigned")),
            ApplicantSignatureDate = reader.IsDBNull(reader.GetOrdinal("ApplicantSignatureDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ApplicantSignatureDate")),
            ParentSigned = reader.GetBoolean(reader.GetOrdinal("ParentSigned")),
            ParentSignatureDate = reader.IsDBNull(reader.GetOrdinal("ParentSignatureDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ParentSignatureDate")),
            ChecklistAllSectionsCompleted = reader.GetBoolean(reader.GetOrdinal("ChecklistAllSectionsCompleted")),
            ChecklistAcademicTranscripts = reader.GetBoolean(reader.GetOrdinal("ChecklistAcademicTranscripts")),
            ChecklistPassportCopy = reader.GetBoolean(reader.GetOrdinal("ChecklistPassportCopy")),
            ChecklistEnglishProficiency = reader.GetBoolean(reader.GetOrdinal("ChecklistEnglishProficiency")),
            ChecklistGSFormSubmitted = reader.GetBoolean(reader.GetOrdinal("ChecklistGSFormSubmitted")),
            ChecklistDeclarationSigned = reader.GetBoolean(reader.GetOrdinal("ChecklistDeclarationSigned")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
        };
    }

    private static ApplicationDocumentResponse MapDocument(SqlDataReader reader)
    {
        return new ApplicationDocumentResponse
        {
            Id = reader.GetGuid(reader.GetOrdinal("Id")),
            ApplicationId = reader.GetGuid(reader.GetOrdinal("ApplicationId")),
            DocCategory = reader.GetString(reader.GetOrdinal("DocCategory")),
            DocType = reader.GetString(reader.GetOrdinal("DocType")),
            FileUrl = reader.GetString(reader.GetOrdinal("FileUrl")),
            IsMandatory = reader.GetBoolean(reader.GetOrdinal("IsMandatory")),
            UploadedAt = reader.GetDateTime(reader.GetOrdinal("UploadedAt"))
        };
    }
    private static StudentApplicationDetailsModel MapStudentApplicationDetail(SqlDataReader reader)
    {
        return new StudentApplicationDetailsModel
        {
            Id = reader.GetGuid(reader.GetOrdinal("Id")),
            ApplicationId = reader.GetGuid(reader.GetOrdinal("ApplicationId")),
            FirstName = reader["FirstName"]?.ToString(),
            LastName = reader["LastName"]?.ToString(),
            Email = reader["Email"]?.ToString(),
            Phone = reader["Phone"]?.ToString(),
            DateOfBirth = reader.IsDBNull(reader.GetOrdinal("DateOfBirth")) ? null : reader.GetDateTime(reader.GetOrdinal("DateOfBirth")),
            Nationality = reader["Nationality"]?.ToString(),
            PassportNumber = reader["PassportNumber"]?.ToString(),
            EmergencyContactName = reader["EmergencyContactName"]?.ToString(),
            EmergencyContactPhone = reader["EmergencyContactPhone"]?.ToString(),
            EmergencyContactRelation = reader["EmergencyContactRelation"]?.ToString(),
            AppliedVisaBefore = reader.GetBoolean(reader.GetOrdinal("AppliedVisaBefore")),
            PreviousVisaType = reader["PreviousVisaType"]?.ToString(),
            RefusedVisa = reader.GetBoolean(reader.GetOrdinal("RefusedVisa")),
            RefusedCountry = reader["RefusedCountry"]?.ToString(),
            RefusedVisaType = reader["RefusedVisaType"]?.ToString(),
            EnglishTestName = reader["EnglishTestName"]?.ToString(),
            EnglishTestScore = reader["EnglishTestScore"]?.ToString(),
            EnglishTestDate = reader.IsDBNull(reader.GetOrdinal("EnglishTestDate")) ? null : reader.GetDateTime(reader.GetOrdinal("EnglishTestDate")),
            HighestQualification = reader["HighestQualification"]?.ToString(),
            UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
            TotalRecords = reader["TotalRecords"] == DBNull.Value
                ? 0
                : Convert.ToInt32(reader["TotalRecords"])
        };
    }
}