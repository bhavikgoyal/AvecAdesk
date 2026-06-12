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

    private static InvoiceResponse MapInvoice(SqlDataReader r) => new()
    {
        InvoiceId = r.GetInt32(r.GetOrdinal("InvoiceId")),
        InvoiceNumber = r.GetString(r.GetOrdinal("InvoiceNumber")),
        InstituteId = r.GetInt32(r.GetOrdinal("InstituteId")),
        TotalAmount = r.GetDecimal(r.GetOrdinal("TotalAmount")),
        Status = r.GetString(r.GetOrdinal("Status")),
        PdfPath = r.IsDBNull(r.GetOrdinal("PdfPath")) ? null : r.GetString(r.GetOrdinal("PdfPath")),
        CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")),
        ApprovedByUserId = r.IsDBNull(r.GetOrdinal("ApprovedByUserId")) ? null : r.GetInt32(r.GetOrdinal("ApprovedByUserId")),
        ApprovedAt = r.IsDBNull(r.GetOrdinal("ApprovedAt")) ? null : r.GetDateTime(r.GetOrdinal("ApprovedAt")),
        RejectionReason = r.IsDBNull(r.GetOrdinal("RejectionReason")) ? null : r.GetString(r.GetOrdinal("RejectionReason"))
    };
}
