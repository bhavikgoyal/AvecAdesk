using AvecADeskApi.Model.Receivables;

namespace AvecADeskApi.Interfaces
{
    public interface IReceivablesRepository
    {
        Task<List<AnticipatedReceivableResponse>> GetAnticipatedAsync(ReceivablesFilter filter);
        Task<MonthRevenueDashboardResponse> GetMonthRevenueDashboardAsync();
        Task<List<StudentPaymentInstallmentResponse>> GetStudentPaymentInstallmentsAsync();
        Task<List<OverdueReceivableResponse>> GetOverdueAsync(ReceivablesFilter filter);
        Task<List<ReceivedPaymentResponse>> GetReceivedAsync(ReceivablesFilter filter);
        Task<ReceivablesSummaryResponse> GetSummaryAsync(ReceivablesFilter filter);
    }
}
