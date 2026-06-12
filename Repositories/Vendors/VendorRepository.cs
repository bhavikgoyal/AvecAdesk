using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.Model.Vendor;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AvecADeskApi.Repositories.Vendors;

public class VendorRepository : IVendorRepository
{
    private readonly SqlDbHelper _db;

    public VendorRepository(SqlDbHelper db)
    {
        _db = db;
    }

    public Task<List<VendorResponse>> GetVendorsAsync(string? status)
    {
        return _db.ExecuteReaderListAsync(
            "sp_GetVendors",
            cmd => cmd.Parameters.AddWithValue("@Status", (object?)status ?? DBNull.Value),
            MapVendor);
    }

    public Task<VendorResponse?> GetVendorByIdAsync(int vendorId)
    {
        return _db.ExecuteReaderSingleAsync(
            "sp_GetVendorById",
            cmd => cmd.Parameters.AddWithValue("@VendorId", vendorId),
            MapVendor);
    }

    public async Task<int> RegisterVendorAsync(VendorRegisterRequest request)
    {
        var vendorIdParam = new SqlParameter("@VendorId", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };

        await _db.ExecuteNonQueryAsync("sp_RegisterVendor", cmd =>
        {
            cmd.Parameters.AddWithValue("@UserId", (object?)request.UserId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BusinessName", request.BusinessName);
            cmd.Parameters.AddWithValue("@ContactPerson", request.ContactPerson);
            cmd.Parameters.AddWithValue("@Phone", request.Phone);
            cmd.Parameters.AddWithValue("@Email", request.Email);
            cmd.Parameters.AddWithValue("@BankDetails", (object?)request.BankDetails ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CommissionPreference", (object?)request.CommissionPreference ?? DBNull.Value);
            cmd.Parameters.Add(vendorIdParam);
        });

        return (int)vendorIdParam.Value;
    }

    public async Task<bool> UpdateVendorAsync(int vendorId, VendorUpdateRequest request)
    {
        var rowsAffectedParam = new SqlParameter("@RowsAffected", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };

        await _db.ExecuteNonQueryAsync("sp_UpdateVendor", cmd =>
        {
            cmd.Parameters.AddWithValue("@VendorId", vendorId);
            cmd.Parameters.AddWithValue("@BusinessName", request.BusinessName);
            cmd.Parameters.AddWithValue("@ContactPerson", request.ContactPerson);
            cmd.Parameters.AddWithValue("@Phone", request.Phone);
            cmd.Parameters.AddWithValue("@Email", request.Email);
            cmd.Parameters.AddWithValue("@BankDetails", (object?)request.BankDetails ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CommissionPreference", (object?)request.CommissionPreference ?? DBNull.Value);
            cmd.Parameters.Add(rowsAffectedParam);
        });

        return (int)rowsAffectedParam.Value > 0;
    }

    public async Task<bool> UpdateVendorStatusAsync(int vendorId, string status)
    {
        var rowsAffectedParam = new SqlParameter("@RowsAffected", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };

        await _db.ExecuteNonQueryAsync("sp_UpdateVendorStatus", cmd =>
        {
            cmd.Parameters.AddWithValue("@VendorId", vendorId);
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.Add(rowsAffectedParam);
        });

        return (int)rowsAffectedParam.Value > 0;
    }

    public Task<VendorResponse?> ApproveVendorAsync(int vendorId, int? approvedByUserId)
    {
        return _db.ExecuteReaderSingleAsync(
            "sp_ApproveVendor",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@VendorId", vendorId);
                cmd.Parameters.AddWithValue("@ApprovedByUserId", (object?)approvedByUserId ?? DBNull.Value);
            },
            MapVendor);
    }

    public Task<VendorAgreementResponse?> GetVendorAgreementAsync(int vendorId)
    {
        return _db.ExecuteReaderSingleAsync(
            "sp_GetVendorAgreement",
            cmd => cmd.Parameters.AddWithValue("@VendorId", vendorId),
            MapVendorAgreement);
    }

    public async Task<int> UploadVendorAgreementAsync(
        int vendorId,
        VendorAgreementUploadRequest request,
        string agreementPath,
        int? uploadedByUserId)
    {
        var agreementIdParam = new SqlParameter("@AgreementId", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };

        await _db.ExecuteNonQueryAsync("sp_UploadVendorAgreement", cmd =>
        {
            cmd.Parameters.AddWithValue("@VendorId", vendorId);
            cmd.Parameters.AddWithValue("@AgreementPath", agreementPath);
            cmd.Parameters.AddWithValue("@AgreementType", request.AgreementType);
            cmd.Parameters.AddWithValue("@SignedAt", (object?)request.SignedAt ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ExpiryDate", (object?)request.ExpiryDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UploadedByUserId", (object?)uploadedByUserId ?? DBNull.Value);
            cmd.Parameters.Add(agreementIdParam);
        });

        return (int)agreementIdParam.Value;
    }

    private static VendorResponse MapVendor(SqlDataReader reader)
    {
        return new VendorResponse
        {
            VendorId = reader.GetInt32(reader.GetOrdinal("VendorId")),
            UserId = reader.IsDBNull(reader.GetOrdinal("UserId")) ? null : reader.GetInt32(reader.GetOrdinal("UserId")),
            VendorCode = reader.IsDBNull(reader.GetOrdinal("VendorCode")) ? null : reader.GetString(reader.GetOrdinal("VendorCode")),
            BusinessName = reader.GetString(reader.GetOrdinal("BusinessName")),
            ContactPerson = reader.GetString(reader.GetOrdinal("ContactPerson")),
            Phone = reader.GetString(reader.GetOrdinal("Phone")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            BankDetails = reader.IsDBNull(reader.GetOrdinal("BankDetails")) ? null : reader.GetString(reader.GetOrdinal("BankDetails")),
            CommissionPreference = reader.IsDBNull(reader.GetOrdinal("CommissionPreference")) ? null : reader.GetString(reader.GetOrdinal("CommissionPreference")),
            Status = reader.GetString(reader.GetOrdinal("Status")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }

    private static VendorAgreementResponse MapVendorAgreement(SqlDataReader reader)
    {
        return new VendorAgreementResponse
        {
            AgreementId = reader.GetInt32(reader.GetOrdinal("AgreementId")),
            VendorId = reader.GetInt32(reader.GetOrdinal("VendorId")),
            AgreementPath = reader.GetString(reader.GetOrdinal("AgreementPath")),
            AgreementType = reader.GetString(reader.GetOrdinal("AgreementType")),
            SignedAt = reader.IsDBNull(reader.GetOrdinal("SignedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("SignedAt")),
            ExpiryDate = reader.IsDBNull(reader.GetOrdinal("ExpiryDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ExpiryDate")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            UploadedByUserId = reader.IsDBNull(reader.GetOrdinal("UploadedByUserId")) ? null : reader.GetInt32(reader.GetOrdinal("UploadedByUserId")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }
}
