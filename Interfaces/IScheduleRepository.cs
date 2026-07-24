using AvecADeskApi.Model.PaymentSchedule;

namespace AvecADeskApi.Interfaces;

public interface IScheduleRepository
{
    Task<List<PaymentScheduleResponse>> GetPaymentSchedulesAsync(int? studentId);
    Task<PaymentScheduleResponse?> GetPaymentScheduleByIdAsync(int scheduleId);
    Task<int> CreatePaymentScheduleAsync(PaymentScheduleCreateRequest request);
    Task<int> CreateStudentPaymentInstallmentAsync(StudentPaymentInstallmentCreateRequest request);
    Task<int> CreateStudentCommissionAsync(StudentCommissionCreateRequest request);
    Task<int> CreateStudentCommissionDetailAsync(StudentCommissionDetailCreateRequest request);
    Task<bool> UpdatePaymentScheduleAsync(int scheduleId, PaymentScheduleUpdateRequest request);
    Task<bool> UpdatePaymentScheduleStatusAsync(int scheduleId, string status, decimal? amountPaid);
    Task<PaymentScheduleBulkStatusResult> BulkUpdatePaymentScheduleStatusAsync(PaymentScheduleBulkStatusRequest request);
    Task<List<StudentPaymentScheduleListResponse>> GetStudentPaymentScheduleListAsync(int? studentId = null);
    Task<UpdateStudentPaymentScheduleResponse> UpdateStudentPaymentScheduleAsync(  UpdateStudentPaymentScheduleRequest request);


    //Task<PaymentScheduleSummaryResponse> GetPaymentSummaryAsync();
    //Task<PaymentScheduleBulkStatusResult> BulkUpdatePaymentScheduleStatusAsync(PaymentScheduleBulkStatusRequest request);
}

