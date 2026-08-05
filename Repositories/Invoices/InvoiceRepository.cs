using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Invoice;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AvecADeskApi.Repositories.Invoices;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly SqlDbHelper _db;
    private readonly LogHelper _logHelper;

    public InvoiceRepository(SqlDbHelper db, LogHelper logHelper)
    {
        _db = db;
        _logHelper = logHelper;
    }

    public async Task<List<InvoiceResponse>> GetInvoicesAsync()
    {
        try
        {
            return await _db.ExecuteReaderListAsync("sp_GetInvoices", _ => { }, MapInvoice);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InvoiceRepository)}.{nameof(GetInvoicesAsync)}", ex);
            throw;
        }
    }
    public async Task<decimal> GetNextMonthInvoiceTotalAsync()
    {
        try
        {
            var result = await _db.ExecuteScalarAsync(
                "sp_GetNextMonthInvoiceTotal",
                cmd => { }
            );

            return result == null || result == DBNull.Value
                ? 0M
                : Convert.ToDecimal(result);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(
                $"{nameof(InvoiceRepository)}.{nameof(GetNextMonthInvoiceTotalAsync)}",
                ex);

            throw;
        }
    }
    public async Task<InvoiceResponse?> GetInvoiceByIdAsync(int invoiceId)
    {
        try
        {
            return await _db.ExecuteReaderSingleAsync("sp_GetInvoiceById",
                cmd => cmd.Parameters.AddWithValue("@InvoiceId", invoiceId), MapInvoice);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InvoiceRepository)}.{nameof(GetInvoiceByIdAsync)}", ex);
            throw;
        }
    }

    public async Task<int> GenerateInvoiceAsync(int uploadId)
    {
        try
        {
            var invoiceIdParam = new SqlParameter("@InvoiceId", SqlDbType.Int) { Direction = ParameterDirection.Output };
            await _db.ExecuteNonQueryAsync("sp_GenerateInvoice", cmd =>
            {
                cmd.Parameters.AddWithValue("@UploadId", uploadId);
                cmd.Parameters.Add(invoiceIdParam);
            });
            return (int)invoiceIdParam.Value;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InvoiceRepository)}.{nameof(GenerateInvoiceAsync)}", ex);
            throw;
        }
    }

    public async Task<bool> SubmitInvoiceAsync(int invoiceId)
    {
        try
        {
            var rowsParam = new SqlParameter("@RowsAffected", SqlDbType.Int) { Direction = ParameterDirection.Output };
            await _db.ExecuteNonQueryAsync("sp_SubmitInvoice", cmd =>
            {
                cmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
                cmd.Parameters.Add(rowsParam);
            });
            return (int)rowsParam.Value > 0;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InvoiceRepository)}.{nameof(SubmitInvoiceAsync)}", ex);
            throw;
        }
    }

    public async Task<InvoiceResponse?> ApproveInvoiceAsync(int invoiceId, int? approvedByUserId)
    {
        try
        {
            return await _db.ExecuteReaderSingleAsync("sp_ApproveInvoice", cmd =>
            {
                cmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
                cmd.Parameters.AddWithValue("@ApprovedByUserId", (object?)approvedByUserId ?? DBNull.Value);
            }, MapInvoice);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InvoiceRepository)}.{nameof(ApproveInvoiceAsync)}", ex);
            throw;
        }
    }

    public async Task<bool> RejectInvoiceAsync(int invoiceId, string rejectionReason)
    {
        try
        {
            var rowsParam = new SqlParameter("@RowsAffected", SqlDbType.Int) { Direction = ParameterDirection.Output };
            await _db.ExecuteNonQueryAsync("sp_RejectInvoice", cmd =>
            {
                cmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
                cmd.Parameters.AddWithValue("@RejectionReason", rejectionReason);
                cmd.Parameters.Add(rowsParam);
            });
            return (int)rowsParam.Value > 0;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InvoiceRepository)}.{nameof(RejectInvoiceAsync)}", ex);
            throw;
        }
    }

    public async Task<string?> GetInvoicePdfPathAsync(int invoiceId)
    {
        try
        {
            var invoice = await GetInvoiceByIdAsync(invoiceId);
            return invoice?.PdfPath;
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InvoiceRepository)}.{nameof(GetInvoicePdfPathAsync)}", ex);
            throw;
        }
    }

    public async Task<List<MonthlyPaidInstallmentRow>> GetPaidInstallmentsForMonthAsync(
        int year,
        int month,
        int? instituteId = null,
        string? campus = null)
    {
        try
        {
            return await _db.ExecuteReaderListAsync(
                "sp_GetPaidInstallmentsForMonthlyInvoice",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@Year", year);
                    cmd.Parameters.AddWithValue("@Month", month);
                    cmd.Parameters.AddWithValue("@InstituteId", (object?)instituteId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Campus", string.IsNullOrWhiteSpace(campus) ? DBNull.Value : campus.Trim());
                },
                MapPaidInstallment);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InvoiceRepository)}.{nameof(GetPaidInstallmentsForMonthAsync)}", ex);
            throw;
        }
    }

    public async Task<List<MonthlyPaidInstallmentRow>> GetInstallmentsForMonthPreviewAsync(
        int year,
        int month,
        int? instituteId = null,
        string? campus = null)
    {
        try
        {
            return await _db.ExecuteReaderListAsync(
                "sp_GetInstallmentsForMonthlyInvoicePreview",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@Year", year);
                    cmd.Parameters.AddWithValue("@Month", month);
                    cmd.Parameters.AddWithValue("@InstituteId", (object?)instituteId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Campus", string.IsNullOrWhiteSpace(campus) ? DBNull.Value : campus.Trim());
                },
                MapPaidInstallment);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InvoiceRepository)}.{nameof(GetInstallmentsForMonthPreviewAsync)}", ex);
            throw;
        }
    }

    public async Task<InvoiceResponse?> GenerateMonthlyPaidStudentInvoiceAsync(
        int year,
        int month,
        int instituteId,
        string? campus = null)
    {
        try
        {
            var invoiceIdParam = new SqlParameter("@InvoiceId", SqlDbType.Int) { Direction = ParameterDirection.Output };
            return await _db.ExecuteReaderSingleAsync(
                "sp_GenerateMonthlyPaidStudentInvoice",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@Year", year);
                    cmd.Parameters.AddWithValue("@Month", month);
                    cmd.Parameters.AddWithValue("@InstituteId", instituteId);
                    cmd.Parameters.AddWithValue("@Campus", string.IsNullOrWhiteSpace(campus) ? DBNull.Value : campus.Trim());
                    cmd.Parameters.Add(invoiceIdParam);
                },
                MapInvoice);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InvoiceRepository)}.{nameof(GenerateMonthlyPaidStudentInvoiceAsync)}", ex);
            throw;
        }
    }

    public async Task UpdateInvoicePdfPathAsync(int invoiceId, string pdfPath)
    {
        try
        {
            await _db.ExecuteNonQueryAsync("sp_UpdateInvoicePdfPath", cmd =>
            {
                cmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
                cmd.Parameters.AddWithValue("@PdfPath", pdfPath);
            });
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InvoiceRepository)}.{nameof(UpdateInvoicePdfPathAsync)}", ex);
            throw;
        }
    }

    public async Task<List<InvoiceLineItemResponse>> GetInvoiceLineItemsAsync(int invoiceId)
    {
        try
        {
            return await _db.ExecuteReaderListAsync(
                "sp_GetInvoiceLineItems",
                cmd => cmd.Parameters.AddWithValue("@InvoiceId", invoiceId),
                r => new InvoiceLineItemResponse
                {
                    LineItemId = r.GetInt32(r.GetOrdinal("LineItemId")),
                    InvoiceId = r.GetInt32(r.GetOrdinal("InvoiceId")),
                    StudentId = r.GetInt32(r.GetOrdinal("StudentId")),
                    StudentName = HasColumn(r, "StudentName") && !r.IsDBNull(r.GetOrdinal("StudentName"))
                    ? r.GetString(r.GetOrdinal("StudentName"))
                    : null,
                    Description = r.IsDBNull(r.GetOrdinal("Description")) ? null : r.GetString(r.GetOrdinal("Description")),
                    Amount = r.GetDecimal(r.GetOrdinal("Amount"))
                });
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(InvoiceRepository)}.{nameof(GetInvoiceLineItemsAsync)}", ex);
            throw;
        }
    }

    private static InvoiceResponse MapInvoice(SqlDataReader r)
    {
        var instituteNameOrdinal = -1;
        try { instituteNameOrdinal = r.GetOrdinal("InstituteName"); } catch { /* optional column */ }

        return new()
        {
            InvoiceId = r.GetInt32(r.GetOrdinal("InvoiceId")),
            InvoiceNumber = r.GetString(r.GetOrdinal("InvoiceNumber")),
            InstituteId = r.GetInt32(r.GetOrdinal("InstituteId")),
            InstituteName = instituteNameOrdinal >= 0 && !r.IsDBNull(instituteNameOrdinal)
                ? r.GetString(instituteNameOrdinal)
                : string.Empty,
            TotalAmount = r.GetDecimal(r.GetOrdinal("TotalAmount")),
            Status = r.GetString(r.GetOrdinal("Status")),
            PdfPath = r.IsDBNull(r.GetOrdinal("PdfPath")) ? null : r.GetString(r.GetOrdinal("PdfPath")),
            CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")),
            ApprovedByUserId = r.IsDBNull(r.GetOrdinal("ApprovedByUserId")) ? null : r.GetInt32(r.GetOrdinal("ApprovedByUserId")),
            ApprovedAt = r.IsDBNull(r.GetOrdinal("ApprovedAt")) ? null : r.GetDateTime(r.GetOrdinal("ApprovedAt")),
            RejectionReason = r.IsDBNull(r.GetOrdinal("RejectionReason")) ? null : r.GetString(r.GetOrdinal("RejectionReason"))
        };
    }

    private static MonthlyPaidInstallmentRow MapPaidInstallment(SqlDataReader r) => new()
    {
        StudentId = r.GetInt32(r.GetOrdinal("StudentId")),
        FullName = r["FullName"]?.ToString() ?? string.Empty,
        StudentCode = r["StudentCode"]?.ToString() ?? string.Empty,
        FolderNo = r["FolderNo"]?.ToString() ?? string.Empty,
        CourseId = r.IsDBNull(r.GetOrdinal("CourseId")) ? null : r.GetInt32(r.GetOrdinal("CourseId")),
        CourseName = r["CourseName"]?.ToString() ?? string.Empty,
        Campus = HasColumn(r, "Campus") ? (r["Campus"]?.ToString() ?? string.Empty) : string.Empty,
        InstituteId = r.GetInt32(r.GetOrdinal("InstituteId")),
        InstituteName = r["InstituteName"]?.ToString() ?? string.Empty,
        ScheduleId = r.GetInt32(r.GetOrdinal("ScheduleId")),
        StudentPaymentInstallmentId = r.GetInt32(r.GetOrdinal("StudentPaymentInstallmentId")),
        InstallmentNo = r.GetInt32(r.GetOrdinal("InstallmentNo")),
        DueDate = r.GetDateTime(r.GetOrdinal("DueDate")),
        FeesAmount = r.GetDecimal(r.GetOrdinal("FeesAmount")),
        PaidAmount = r.GetDecimal(r.GetOrdinal("PaidAmount")),
        PaidDate = r.IsDBNull(r.GetOrdinal("PaidDate")) ? null : r.GetDateTime(r.GetOrdinal("PaidDate")),
        PaymentStatus = r["PaymentStatus"]?.ToString() ?? string.Empty,
        CommissionDetailId = r.IsDBNull(r.GetOrdinal("CommissionDetailId")) ? null : r.GetInt32(r.GetOrdinal("CommissionDetailId")),
        CommissionAmount = r.GetDecimal(r.GetOrdinal("CommissionAmount")),
        GSTAmount = r.GetDecimal(r.GetOrdinal("GSTAmount")),
        BonusAmount = r.GetDecimal(r.GetOrdinal("BonusAmount")),
        InvoiceAmount = r.GetDecimal(r.GetOrdinal("InvoiceAmount")),
        GSTPercentage = r.GetDecimal(r.GetOrdinal("GSTPercentage"))
    };

    private static bool HasColumn(SqlDataReader reader, string column)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(reader.GetName(i), column, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
