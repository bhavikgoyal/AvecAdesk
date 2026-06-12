using AvecADeskApi.Model.PaymentSchedule;

namespace AvecADeskApi.Interfaces;

public interface IScheduleRepository
{
    Task<List<PaymentScheduleResponse>> GetPaymentSchedulesAsync(int? studentId);
    Task<PaymentScheduleResponse?> GetPaymentScheduleByIdAsync(int scheduleId);
    Task<int> CreatePaymentScheduleAsync(PaymentScheduleCreateRequest request);
    Task<bool> UpdatePaymentScheduleAsync(int scheduleId, PaymentScheduleUpdateRequest request);
    Task<bool> UpdatePaymentScheduleStatusAsync(int scheduleId, string status, decimal? amountPaid);
    Task<int> BulkUpdatePaymentScheduleStatusAsync(PaymentScheduleBulkStatusRequest request);
}
