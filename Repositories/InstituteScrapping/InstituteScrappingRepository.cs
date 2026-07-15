using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.InstituteScrapping;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AvecADeskApi.Repositories.InstituteScrapping;

public class InstituteScrappingRepository : IInstituteScrappingRepository
{
    private readonly SqlDbHelper _db;
    private readonly LogHelper _logHelper;

    public InstituteScrappingRepository(SqlDbHelper db, LogHelper logHelper)
    {
        _db = db;
        _logHelper = logHelper;
    }

    public async Task<List<InstituteScrappingResponse>> GetAllAsync(string? instituteName = null)
    {
        try
        {
            var rows = await _db.ExecuteReaderListAsync("sp_GetInstituteScrapping", _ => { }, MapRow);
            if (string.IsNullOrWhiteSpace(instituteName))
                return rows;

            var query = instituteName.Trim();
            return rows
                .Where(row => !string.IsNullOrWhiteSpace(row.InstituteName)
                    && row.InstituteName.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InstituteScrappingRepository)}.{nameof(GetAllAsync)}", ex);
            throw;
        }
    }
    public async Task<List<InstituteNameResponse>> GetUniqueInstituteNamesAsync()
    {
        try
        {
            return await _db.ExecuteReaderListAsync(
                "sp_GetUniqueInstituteNames", _ => { }, reader => new InstituteNameResponse
                {
                    ScrappingId = reader.GetInt32( reader.GetOrdinal("ScrappingId")),
                    InstituteName = ReadString(reader, "InstituteName")
                });
        }
        catch (Exception ex)
        {
            _logHelper.LogError( $"{nameof(InstituteScrappingRepository)}.{nameof(GetUniqueInstituteNamesAsync)}", ex);
             throw;
        }
    }
    public async Task<InstituteScrappingResponse?> GetByIdAsync(int scrappingId)
    {
        try
        {
            return await _db.ExecuteReaderSingleAsync(
                "sp_GetInstituteScrappingById",
                cmd => cmd.Parameters.AddWithValue("@ScrappingId", scrappingId),
                MapRow);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InstituteScrappingRepository)}.{nameof(GetByIdAsync)}", ex);
            throw;
        }
    }

    public async Task<int> CreateAsync(InstituteScrappingUpsertRequest request)
    {
        try
        {
            var idParam = new SqlParameter("@ScrappingId", SqlDbType.Int) { Direction = ParameterDirection.Output };
            await _db.ExecuteNonQueryAsync("sp_CreateInstituteScrapping", cmd =>
            {
                AddUpsertParameters(cmd, request);
                cmd.Parameters.Add(idParam);
            });
            return (int)idParam.Value;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InstituteScrappingRepository)}.{nameof(CreateAsync)}", ex);
            throw;
        }
    }

    public async Task<InstituteScrappingManualCreateResult> ManualCreateAsync(InstituteScrappingUpsertRequest request, int? vendorId = null)
    {
        try
        {
            var scrappingIdParam = new SqlParameter("@ScrappingId", SqlDbType.Int) { Direction = ParameterDirection.Output };
            var courseIdParam = new SqlParameter("@CourseId", SqlDbType.Int) { Direction = ParameterDirection.Output };
            var instituteIdParam = new SqlParameter("@InstituteId", SqlDbType.Int) { Direction = ParameterDirection.Output };

            await _db.ExecuteNonQueryAsync("sp_ManualCreateInstituteScrapping", cmd =>
            {
                AddUpsertParameters(cmd, request);
                cmd.Parameters.AddWithValue("@VendorId", (object?)vendorId ?? DBNull.Value);
                cmd.Parameters.Add(scrappingIdParam);
                cmd.Parameters.Add(courseIdParam);
                cmd.Parameters.Add(instituteIdParam);
            });

            var scrappingId = (int)scrappingIdParam.Value;
            return new InstituteScrappingManualCreateResult
            {
                ScrappingId = scrappingId,
                CourseId = (int)courseIdParam.Value,
                InstituteId = (int)instituteIdParam.Value,
                Record = await GetByIdAsync(scrappingId),
            };
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InstituteScrappingRepository)}.{nameof(ManualCreateAsync)}", ex);
            throw;
        }
    }

    public async Task<List<InstituteScrappingResponse>> CreateManyAsync(IEnumerable<InstituteScrappingUpsertRequest> requests)
    {
        var records = new List<InstituteScrappingResponse>();
        foreach (var request in requests)
        {
            var id = await CreateAsync(request);
            var record = await GetByIdAsync(id);
            if (record != null)
                records.Add(record);
        }
        return records;
    }

    public async Task<bool> UpdateAsync(int scrappingId, InstituteScrappingUpsertRequest request)
    {
        try
        {
            var rowsParam = new SqlParameter("@RowsAffected", SqlDbType.Int) { Direction = ParameterDirection.Output };
            await _db.ExecuteNonQueryAsync("sp_UpdateInstituteScrapping", cmd =>
            {
                cmd.Parameters.AddWithValue("@ScrappingId", scrappingId);
                AddUpsertParameters(cmd, request);
                cmd.Parameters.Add(rowsParam);
            });
            return (int)rowsParam.Value > 0;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InstituteScrappingRepository)}.{nameof(UpdateAsync)}", ex);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int scrappingId)
    {
        try
        {
            var rowsParam = new SqlParameter("@RowsAffected", SqlDbType.Int) { Direction = ParameterDirection.Output };
            await _db.ExecuteNonQueryAsync("sp_DeleteInstituteScrapping", cmd =>
            {
                cmd.Parameters.AddWithValue("@ScrappingId", scrappingId);
                cmd.Parameters.Add(rowsParam);
            });
            return (int)rowsParam.Value > 0;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InstituteScrappingRepository)}.{nameof(DeleteAsync)}", ex);
            throw;
        }
    }

    private static void AddUpsertParameters(SqlCommand cmd, InstituteScrappingUpsertRequest request)
    {
        cmd.Parameters.AddWithValue("@InstituteName", (object?)request.InstituteName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@WebsiteURL", (object?)request.WebsiteURL ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Campus", (object?)request.Campus ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@State", (object?)request.State ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ProgramName", (object?)request.ProgramName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Level", (object?)request.Level ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ProgramLink", (object?)request.ProgramLink ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CricosCode", (object?)request.CricosCode ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Duration", (object?)request.Duration ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Intake", (object?)request.Intake ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@FeesYearly", (object?)request.FeesYearly ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@EnglishReq", (object?)request.EnglishReq ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Name", (object?)request.Name ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Logo", (object?)request.Logo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Country", (object?)request.Country ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@City", (object?)request.City ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CountryRanking", (object?)request.CountryRanking ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ScholarshipsDetails", (object?)request.ScholarshipsDetails ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ProgramDescription", (object?)request.ProgramDescription ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ProgramLogo", (object?)request.ProgramLogo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@AddmissionRequirements", (object?)request.AddmissionRequirements ?? DBNull.Value);
    }

    private static InstituteScrappingResponse MapRow(SqlDataReader reader)
    {
        return new InstituteScrappingResponse
        {
            ScrappingId = reader.GetInt32(reader.GetOrdinal("ScrappingId")),
            InstituteName = ReadString(reader, "InstituteName"),
            WebsiteURL = ReadString(reader, "WebsiteURL"),
            Campus = ReadString(reader, "Campus"),
            State = ReadString(reader, "State"),
            ProgramName = ReadString(reader, "ProgramName"),
            Level = ReadString(reader, "Level"),
            ProgramLink = ReadString(reader, "ProgramLink"),
            CricosCode = ReadString(reader, "CricosCode"),
            Duration = ReadString(reader, "Duration"),
            Intake = ReadString(reader, "Intake"),
            FeesYearly = ReadString(reader, "FeesYearly"),
            EnglishReq = ReadString(reader, "EnglishReq"),
            Name = ReadString(reader, "Name"),
            Logo = ReadString(reader, "Logo"),
            Country = ReadString(reader, "Country"),
            City = ReadString(reader, "City"),
            Description = ReadString(reader, "Description"),
            CountryRanking = ReadString(reader, "CountryRanking"),
            ScholarshipsDetails = ReadString(reader, "ScholarshipsDetails"),
            ProgramDescription = ReadString(reader, "ProgramDescription"),
            ProgramLogo = ReadString(reader, "ProgramLogo"),
            AddmissionRequirements = ReadString(reader, "AddmissionRequirements"),
            IsScrap = ReadBoolean(reader, "IsScrap"),
            CreatedAt = ReadDateTime(reader, "CreatedAt"),
        };
    }

    private static bool? ReadBoolean(SqlDataReader reader, string column)
    {
        try
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? null : reader.GetBoolean(ordinal);
        }
        catch (IndexOutOfRangeException)
        {
            return null;
        }
    }

    private static string? ReadString(SqlDataReader reader, string column)
    {
        try
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }
        catch (IndexOutOfRangeException)
        {
            return null;
        }
    }

    private static DateTime? ReadDateTime(SqlDataReader reader, string column)
    {
        try
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
        }
        catch (IndexOutOfRangeException)
        {
            return null;
        }
    }
}
