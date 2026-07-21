using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.VendorStudent;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AvecADeskApi.Repositories.VendorStudent
{
  public class VendorStudentRepository : IVendorStudentRepository
  {
    private readonly SqlDbHelper _db;
    private readonly LogHelper _logHelper;

    public VendorStudentRepository(
        SqlDbHelper db,
        LogHelper logHelper)
    {
      _db = db;
      _logHelper = logHelper;
    }

    public async Task<int> CreateVendorStudentAsync(SaveVendorStudentRequest request)
    {
      try
      {
        var newStudentIdParam = new SqlParameter("@NewStudentID", SqlDbType.Int)
        {
          Direction = ParameterDirection.Output
        };

        await _db.ExecuteNonQueryAsync("dbo.SP_CreateVendorStudent", cmd =>
        {
            cmd.Parameters.AddWithValue("@VendorID", (object?)request.VendorID ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@InstituteID", (object?)request.InstituteID ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@CourseID", (object?)request.CourseID ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@CountryToApply", (object?)request.CountryToApply ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@FirstName", (object?)request.FirstName ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@LastName", (object?)request.LastName ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@Email", (object?)request.Email ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@MobileNumber", (object?)request.MobileNumber ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@Title", (object?)request.Title ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@FamilyName", (object?)request.FamilyName ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@GivenNames", (object?)request.GivenNames ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@PreviousName", (object?)request.PreviousName ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@DateOfBirth", (object?)request.DateOfBirth ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@Gender", (object?)request.Gender ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@CountryOfBirth", (object?)request.CountryOfBirth ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@Citizenship", (object?)request.Citizenship ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@PassportNumber", (object?)request.PassportNumber ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@PassportExpiryDate", (object?)request.PassportExpiryDate ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@PassportCountryOfIssue", (object?)request.PassportCountryOfIssue ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@PassportFilePath", (object?)request.PassportFilePath ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@CurrentAddress", (object?)request.CurrentAddress ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@CurrentSuburb", (object?)request.CurrentSuburb ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@CurrentState", (object?)request.CurrentState ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@CurrentCountry", (object?)request.CurrentCountry ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@CurrentPostcode", (object?)request.CurrentPostcode ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@EmergencyContactName", (object?)request.EmergencyContactName ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@EmergencyContactRelationship", (object?)request.EmergencyContactRelationship ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@EmergencyContactPhone", (object?)request.EmergencyContactPhone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@EmergencyContactEmail", (object?)request.EmergencyContactEmail ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SourceStudentID", (object?)request.SourceStudentID ?? DBNull.Value);
            cmd.Parameters.Add(newStudentIdParam);
        });

        return Convert.ToInt32(newStudentIdParam.Value);
      }
      catch (Exception ex)
      {
        _logHelper.LogError($"{nameof(VendorStudentRepository)}.{nameof(CreateVendorStudentAsync)}", ex);
        throw;
      }
    }

    public async Task UpdateAgentDetailsAsync(int studentId, UpdateVendorStudentAgentRequest request)
    {
      try
      {
        await _db.ExecuteNonQueryAsync("dbo.SP_UpdateVendorStudentAgent", cmd =>
        {
          cmd.Parameters.AddWithValue("@StudentID", studentId);
          cmd.Parameters.AddWithValue("@AgentAgencyName", (object?)request.AgentAgencyName ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@AgentContactPerson", (object?)request.AgentContactPerson ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@AgentEmail", (object?)request.AgentEmail ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@AgentTelephone", (object?)request.AgentTelephone ?? DBNull.Value);
        });
      }
      catch (Exception ex)
      {
        _logHelper.LogError($"{nameof(VendorStudentRepository)}.{nameof(UpdateAgentDetailsAsync)}", ex);
        throw;
      }
    }

    public async Task UpdateImmigrationAsync(int studentId, UpdateVendorStudentImmigrationRequest request)
    {
      try
      {
        await _db.ExecuteNonQueryAsync("dbo.SP_UpdateVendorStudentImmigration", cmd =>
        {
          cmd.Parameters.AddWithValue("@StudentID", studentId);
          cmd.Parameters.AddWithValue("@VisaAppliedBefore", (object?)request.VisaAppliedBefore ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@VisaAppliedType", (object?)request.VisaAppliedType ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@VisaRefused", (object?)request.VisaRefused ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@RefusedVisaCountry", (object?)request.RefusedVisaCountry ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@RefusedVisaType", (object?)request.RefusedVisaType ?? DBNull.Value);
        });
      }
      catch (Exception ex)
      {
        _logHelper.LogError($"{nameof(VendorStudentRepository)}.{nameof(UpdateImmigrationAsync)}", ex);
        throw;
      }
    }

    public async Task UpdateEnglishAsync(int studentId, UpdateVendorStudentEnglishRequest request)
    {
      try
      {
        await _db.ExecuteNonQueryAsync("dbo.SP_UpdateVendorStudentEnglish", cmd =>
        {
          cmd.Parameters.AddWithValue("@StudentID", studentId);
          cmd.Parameters.AddWithValue("@EnglishTestType", (object?)request.EnglishTestType ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@EnglishOverallScore", (object?)request.EnglishOverallScore ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@EnglishTestDate", (object?)request.EnglishTestDate ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@EnglishEvidenceFilePath", (object?)request.EnglishEvidenceFilePath ?? DBNull.Value);
        });
      }
      catch (Exception ex)
      {
        _logHelper.LogError($"{nameof(VendorStudentRepository)}.{nameof(UpdateEnglishAsync)}", ex);
        throw;
      }
    }

    public async Task SaveEducationHistoryAsync(int studentId, SaveStudentEducationHistoryRequest request)
    {
      try
      {
        await _db.ExecuteNonQueryAsync("dbo.SP_SaveStudentEducationHistory", cmd =>
        {
          cmd.Parameters.AddWithValue("@StudentID", studentId);
          cmd.Parameters.AddWithValue("@HighestQualification", (object?)request.HighestQualification ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@StudiedHighSchoolAustralia", (object?)request.StudiedHighSchoolAustralia ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@HasSecondaryPostSecondaryQual", (object?)request.HasSecondaryPostSecondaryQual ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@HighSchoolDetails", (object?)request.HighSchoolDetails ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@QualificationDetails", (object?)request.QualificationDetails ?? DBNull.Value);
        });
      }
      catch (Exception ex)
      {
        _logHelper.LogError($"{nameof(VendorStudentRepository)}.{nameof(SaveEducationHistoryAsync)}", ex);
        throw;
      }
    }

    public async Task<int> SaveDocumentAsync(int studentId, string category, string docType, string filePath)
    {
      try
      {
        var newDocIdParam = new SqlParameter("@NewDocumentID", SqlDbType.Int)
        {
          Direction = ParameterDirection.Output
        };

        await _db.ExecuteNonQueryAsync("dbo.SP_SaveStudentDocument", cmd =>
        {
          cmd.Parameters.AddWithValue("@StudentID", studentId);
          cmd.Parameters.AddWithValue("@DocumentCategory", category);
          cmd.Parameters.AddWithValue("@DocumentType", docType);
          cmd.Parameters.AddWithValue("@FilePath", filePath);
          cmd.Parameters.Add(newDocIdParam);
        });

        return Convert.ToInt32(newDocIdParam.Value);
      }
      catch (Exception ex)
      {
        _logHelper.LogError($"{nameof(VendorStudentRepository)}.{nameof(SaveDocumentAsync)}", ex);
        throw;
      }
    }

    public async Task UpdateChecklistAsync(int studentId, UpdateVendorStudentChecklistRequest request)
    {
      try
      {
        await _db.ExecuteNonQueryAsync("dbo.SP_UpdateVendorStudentChecklist", cmd =>
        {
          cmd.Parameters.AddWithValue("@StudentID", studentId);
          cmd.Parameters.AddWithValue("@ChkCompletedAllSections", request.ChkCompletedAllSections);
          cmd.Parameters.AddWithValue("@ChkAgentCertifiedTranscripts", request.ChkAgentCertifiedTranscripts);
          cmd.Parameters.AddWithValue("@ChkAgentCertifiedPassport", request.ChkAgentCertifiedPassport);
          cmd.Parameters.AddWithValue("@ChkEnglishProficiencyEvidence", request.ChkEnglishProficiencyEvidence);
          cmd.Parameters.AddWithValue("@ChkGSAssessmentFormSubmitted", request.ChkGSAssessmentFormSubmitted);
          cmd.Parameters.AddWithValue("@ChkReadSignedDeclaration", request.ChkReadSignedDeclaration);
        });
      }
      catch (Exception ex)
      {
        _logHelper.LogError($"{nameof(VendorStudentRepository)}.{nameof(UpdateChecklistAsync)}", ex);
        throw;
      }
    }

    public async Task UpdateDeclarationAsync(int studentId, UpdateVendorStudentDeclarationRequest request)
    {
      try
      {
        await _db.ExecuteNonQueryAsync("dbo.SP_UpdateVendorStudentDeclaration", cmd =>
        {
          cmd.Parameters.AddWithValue("@StudentID", studentId);
          cmd.Parameters.AddWithValue("@DeclarationName", (object?)request.DeclarationName ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@ApplicantSignaturePath", (object?)request.ApplicantSignaturePath ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@ApplicantSignatureDate", (object?)request.ApplicantSignatureDate ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@ParentGuardianName", (object?)request.ParentGuardianName ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@ParentSignaturePath", (object?)request.ParentSignaturePath ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@ParentSignatureDate", (object?)request.ParentSignatureDate ?? DBNull.Value);
        });
      }
      catch (Exception ex)
      {
        _logHelper.LogError($"{nameof(VendorStudentRepository)}.{nameof(UpdateDeclarationAsync)}", ex);
        throw;
      }
    }

    public async Task SubmitAsync(int studentId)
    {
      try
      {
        await _db.ExecuteNonQueryAsync("dbo.SP_SubmitVendorStudent", cmd =>
        {
          cmd.Parameters.AddWithValue("@StudentID", studentId);
        });
      }
      catch (Exception ex)
      {
        _logHelper.LogError($"{nameof(VendorStudentRepository)}.{nameof(SubmitAsync)}", ex);
        throw;
      }
    }

    public async Task<List<VendorStudentHistoryItem>> GetHistoryAsync(int vendorId, string? search, int pageNumber, int pageSize)
    {
      try
      {
        return await _db.ExecuteReaderListAsync(
            "dbo.SP_GetVendorStudentHistory",
            cmd =>
            {
              cmd.Parameters.AddWithValue("@VendorID", vendorId);
              cmd.Parameters.AddWithValue("@Search", (object?)search ?? DBNull.Value);
              cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
              cmd.Parameters.AddWithValue("@PageSize", pageSize);
            },
            MapHistoryItem);
      }
      catch (Exception ex)
      {
        _logHelper.LogError($"{nameof(VendorStudentRepository)}.{nameof(GetHistoryAsync)}", ex);
        throw;
      }
    }

    private static VendorStudentHistoryItem MapHistoryItem(SqlDataReader reader)
    {
      var firstName = GetNullableString(reader, "FirstName");
      var lastName = GetNullableString(reader, "LastName");
      var fullName = GetNullableString(reader, "FullName");
      if (string.IsNullOrWhiteSpace(fullName))
        fullName = string.Join(" ", new[] { firstName, lastName }.Where(x => !string.IsNullOrWhiteSpace(x)));

      return new VendorStudentHistoryItem
      {
        StudentID = reader.GetInt32(reader.GetOrdinal("StudentID")),
        FirstName = firstName,
        LastName = lastName,
        FullName = fullName,
        Email = GetNullableString(reader, "Email"),
        MobileNumber = GetNullableString(reader, "MobileNumber"),
        CountryToApply = GetNullableString(reader, "CountryToApply"),
        CourseName = GetNullableString(reader, "CourseName"),
        ApplicationStatus = GetNullableString(reader, "ApplicationStatus"),
        SubmittedDate = GetNullableDateTime(reader, "SubmittedDate"),
        TotalRecords = GetNullableInt(reader, "TotalRecords") ?? 0
      };
    }

    public async Task<VendorStudentDetailResponse?> GetByIdAsync(int studentId)
    {
      try
      {
        var student = await _db.ExecuteReaderSingleAsync(
            "dbo.SP_GetVendorStudentById",
            cmd => cmd.Parameters.AddWithValue("@StudentID", studentId),
            MapVendorStudentDetail);

        if (student == null)
          return null;

        student.EducationHistory = await _db.ExecuteReaderListAsync(
            "dbo.SP_GetStudentEducationByStudentId",
            cmd => cmd.Parameters.AddWithValue("@StudentID", studentId),
            MapEducationItem);

        student.Documents = await _db.ExecuteReaderListAsync(
            "dbo.SP_GetStudentDocumentsByStudentId",
            cmd => cmd.Parameters.AddWithValue("@StudentID", studentId),
            MapDocumentItem);

        return student;
      }
      catch (Exception ex)
      {
        _logHelper.LogError($"{nameof(VendorStudentRepository)}.{nameof(GetByIdAsync)}", ex);
        throw;
      }
    }
        public async Task<List<VendorStudentHistoryItem>> GetStudentApplicationListAsync(string? search, int pageNumber, int pageSize)
        {
            try
            {
                return await _db.ExecuteReaderListAsync(
                "dbo.SP_GetStudentApplicationList",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@Search", (object?)search ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                },
                reader => new VendorStudentHistoryItem
                {
                    StudentID = reader.GetInt32(reader.GetOrdinal("StudentID")),
                    FirstName = reader.IsDBNull(reader.GetOrdinal("FirstName")) ? null : reader.GetString(reader.GetOrdinal("FirstName")),
                    LastName = reader.IsDBNull(reader.GetOrdinal("LastName")) ? null : reader.GetString(reader.GetOrdinal("LastName")),
                    Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
                    CountryToApply = reader.IsDBNull(reader.GetOrdinal("CountryToApply")) ? null : reader.GetString(reader.GetOrdinal("CountryToApply")),
                    Phone = reader.IsDBNull(reader.GetOrdinal("MobileNumber")) ? null : reader.GetString(reader.GetOrdinal("MobileNumber")),
                    EnglishTest = reader.IsDBNull(reader.GetOrdinal("EnglishTestType")) ? null : reader.GetString(reader.GetOrdinal("EnglishTestType")),
                    TestScore = reader.IsDBNull(reader.GetOrdinal("TestScore")) ? null : reader.GetString(reader.GetOrdinal("TestScore")),
                    HighestQualification = null,
                    //HighestQualification = reader.IsDBNull(reader.GetOrdinal("HighestQualification")) ? null : reader.GetString(reader.GetOrdinal("HighestQualification")),
                    ApplicationStatus = reader.IsDBNull(reader.GetOrdinal("ApplicationStatus")) ? null : reader.GetString(reader.GetOrdinal("ApplicationStatus")),
                    SubmittedDate = reader.IsDBNull(reader.GetOrdinal("SubmittedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("SubmittedDate")),
                    TotalRecords = reader.GetInt32(reader.GetOrdinal("TotalRecords"))
                });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(VendorStudentRepository)}.{nameof(GetStudentApplicationListAsync)}", ex);
                throw;
            }
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

    private static string? GetNullableString(SqlDataReader reader, string column)
    {
      if (!HasColumn(reader, column)) return null;
      var ordinal = reader.GetOrdinal(column);
      return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static DateTime? GetNullableDateTime(SqlDataReader reader, string column)
    {
      if (!HasColumn(reader, column)) return null;
      var ordinal = reader.GetOrdinal(column);
      return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }

    private static bool? GetNullableBool(SqlDataReader reader, string column)
    {
      if (!HasColumn(reader, column)) return null;
      var ordinal = reader.GetOrdinal(column);
      return reader.IsDBNull(ordinal) ? null : reader.GetBoolean(ordinal);
    }

    private static int? GetNullableInt(SqlDataReader reader, string column)
    {
      if (!HasColumn(reader, column)) return null;
      var ordinal = reader.GetOrdinal(column);
      return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

    private static VendorStudentDetailResponse MapVendorStudentDetail(SqlDataReader reader)
    {
      return new VendorStudentDetailResponse
      {
        StudentID = reader.GetInt32(reader.GetOrdinal("StudentID")),
        VendorID = GetNullableInt(reader, "VendorID"),
        InstituteID = GetNullableInt(reader, "InstituteID"),
        CourseID = GetNullableInt(reader, "CourseID"),
        CourseName = GetNullableString(reader, "CourseName"),
        CountryToApply = GetNullableString(reader, "CountryToApply"),
        FirstName = GetNullableString(reader, "FirstName"),
        LastName = GetNullableString(reader, "LastName"),
        Email = GetNullableString(reader, "Email"),
        MobileNumber = GetNullableString(reader, "MobileNumber"),
        Title = GetNullableString(reader, "Title"),
        FamilyName = GetNullableString(reader, "FamilyName"),
        GivenNames = GetNullableString(reader, "GivenNames"),
        PreviousName = GetNullableString(reader, "PreviousName"),
        DateOfBirth = GetNullableDateTime(reader, "DateOfBirth"),
        Gender = GetNullableString(reader, "Gender"),
        CountryOfBirth = GetNullableString(reader, "CountryOfBirth"),
        Citizenship = GetNullableString(reader, "Citizenship"),
        PassportNumber = GetNullableString(reader, "PassportNumber"),
        PassportExpiryDate = GetNullableDateTime(reader, "PassportExpiryDate"),
        PassportCountryOfIssue = GetNullableString(reader, "PassportCountryOfIssue"),
        PassportFilePath = GetNullableString(reader, "PassportFilePath"),
        CurrentAddress = GetNullableString(reader, "CurrentAddress"),
        CurrentSuburb = GetNullableString(reader, "CurrentSuburb"),
        CurrentState = GetNullableString(reader, "CurrentState"),
        CurrentCountry = GetNullableString(reader, "CurrentCountry"),
        CurrentPostcode = GetNullableString(reader, "CurrentPostcode"),
        EmergencyContactName = GetNullableString(reader, "EmergencyContactName"),
        EmergencyContactRelationship = GetNullableString(reader, "EmergencyContactRelationship"),
        EmergencyContactPhone = GetNullableString(reader, "EmergencyContactPhone"),
        EmergencyContactEmail = GetNullableString(reader, "EmergencyContactEmail"),
        AgentAgencyName = GetNullableString(reader, "AgentAgencyName"),
        AgentContactPerson = GetNullableString(reader, "AgentContactPerson"),
        AgentEmail = GetNullableString(reader, "AgentEmail"),
        AgentTelephone = GetNullableString(reader, "AgentTelephone"),
        VisaAppliedBefore = GetNullableBool(reader, "VisaAppliedBefore"),
        VisaAppliedType = GetNullableString(reader, "VisaAppliedType"),
        VisaRefused = GetNullableBool(reader, "VisaRefused"),
        RefusedVisaCountry = GetNullableString(reader, "RefusedVisaCountry"),
        RefusedVisaType = GetNullableString(reader, "RefusedVisaType"),
        EnglishTestType = GetNullableString(reader, "EnglishTestType"),
        EnglishOverallScore = GetNullableString(reader, "EnglishOverallScore"),
        EnglishTestDate = GetNullableDateTime(reader, "EnglishTestDate"),
        EnglishEvidenceFilePath = GetNullableString(reader, "EnglishEvidenceFilePath"),
        ChkCompletedAllSections = GetNullableBool(reader, "ChkCompletedAllSections"),
        ChkAgentCertifiedTranscripts = GetNullableBool(reader, "ChkAgentCertifiedTranscripts"),
        ChkAgentCertifiedPassport = GetNullableBool(reader, "ChkAgentCertifiedPassport"),
        ChkEnglishProficiencyEvidence = GetNullableBool(reader, "ChkEnglishProficiencyEvidence"),
        ChkGSAssessmentFormSubmitted = GetNullableBool(reader, "ChkGSAssessmentFormSubmitted"),
        ChkReadSignedDeclaration = GetNullableBool(reader, "ChkReadSignedDeclaration"),
        DeclarationName = GetNullableString(reader, "DeclarationName"),
        ApplicantSignaturePath = GetNullableString(reader, "ApplicantSignaturePath"),
        ApplicantSignatureDate = GetNullableDateTime(reader, "ApplicantSignatureDate"),
        ParentGuardianName = GetNullableString(reader, "ParentGuardianName"),
        ParentSignaturePath = GetNullableString(reader, "ParentSignaturePath"),
        ParentSignatureDate = GetNullableDateTime(reader, "ParentSignatureDate"),
        SubmittedDate = GetNullableDateTime(reader, "SubmittedDate"),
        ApplicationStatus = GetNullableString(reader, "ApplicationStatus")
      };
    }

    private static StudentEducationItem MapEducationItem(SqlDataReader reader)
    {
      return new StudentEducationItem
      {
        RecordID = reader.GetInt32(reader.GetOrdinal("RecordID")),
        StudentID = reader.GetInt32(reader.GetOrdinal("StudentID")),
        HighestQualification = GetNullableString(reader, "HighestQualification"),
        StudiedHighSchoolAustralia = GetNullableBool(reader, "StudiedHighSchoolAustralia"),
        HasSecondaryPostSecondaryQual = GetNullableBool(reader, "HasSecondaryPostSecondaryQual"),
        RecordType = GetNullableString(reader, "RecordType"),
        InstitutionSchool = GetNullableString(reader, "InstitutionSchool"),
        Course = GetNullableString(reader, "Course"),
        LocationDetail = GetNullableString(reader, "LocationDetail"),
        YearCommencedCompleted = GetNullableString(reader, "YearCommencedCompleted")
      };
    }

    private static StudentDocumentItem MapDocumentItem(SqlDataReader reader)
    {
      return new StudentDocumentItem
      {
        DocumentID = reader.GetInt32(reader.GetOrdinal("DocumentID")),
        StudentID = reader.GetInt32(reader.GetOrdinal("StudentID")),
        DocumentCategory = GetNullableString(reader, "DocumentCategory"),
        DocumentType = GetNullableString(reader, "DocumentType"),
        FilePath = GetNullableString(reader, "FilePath"),
        UploadedDate = GetNullableDateTime(reader, "UploadedDate")
      };
    }
  }
}
