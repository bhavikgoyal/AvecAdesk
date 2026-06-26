using AvecADeskApi.Helpers;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.DuePayment;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AvecADeskApi.Repositories.DuePayment
    ;

public class DuePaymentRepository : IDuePaymentRepository
{
    private readonly SqlDbHelper _db;
    private readonly LogHelper _logHelper;

    public DuePaymentRepository(SqlDbHelper db, LogHelper logHelper)
    {
        _db = db;
        _logHelper = logHelper;
    }

    public async Task<List<DuePaymentResponse>> GetDuePaymentsAsync()
    {
        try
        {
            return await _db.ExecuteReaderListAsync(
                "sp_Student_DuePayments",
                null,
                MapDuePayment);
        }
        catch (Exception ex)
        {
            _logHelper.LogError($"{nameof(DuePaymentRepository)}.{nameof(GetDuePaymentsAsync)}", ex);
            throw;
        }
    }

    private DuePaymentResponse MapDuePayment(IDataReader reader)
    {
        return new DuePaymentResponse
        {
            ScheduleId = Convert.ToInt32(reader["ScheduleId"]),
            StudentId = Convert.ToInt32(reader["StudentId"]),
            FullName = reader["FullName"]?.ToString() ?? string.Empty,
            Email = reader["Email"]?.ToString() ?? string.Empty,
            EnrollmentNumber = reader["EnrollmentNumber"]?.ToString() ?? string.Empty,
            EnrolmentStatus = reader["EnrolmentStatus"]?.ToString() ?? string.Empty,
            Phone = reader["Phone"]?.ToString() ?? string.Empty,
            DueDate = Convert.ToDateTime(reader["DueDate"]),
            TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
            AmountPaid = Convert.ToDecimal(reader["AmountPaid"]),
            DueAmount = Convert.ToDecimal(reader["DueAmount"]),
            Status = reader["Status"]?.ToString() ?? string.Empty
        };
    }
}
