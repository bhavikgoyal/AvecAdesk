using AvecADeskApi.Model.Reminder;

namespace AvecADeskApi.Interfaces;

public interface ICommissionReportRequestRepository
{
    Task<List<CommissionReportRequestRecipient>> GetRecipientsAsync(DateTime reportMonth);

    Task LogAsync(DateTime reportMonth, CommissionReportRequestRecipient college, string status, string? errorMessage);
}
