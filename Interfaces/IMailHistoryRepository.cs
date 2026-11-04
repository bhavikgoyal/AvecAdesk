using AvecADeskApi.Model.MailHistory;

namespace AvecADeskApi.Interfaces;

public interface IMailHistoryRepository
{
    Task AddAsync(MailHistoryEntry entry);
    Task<MailHistoryPagedResult> GetPagedAsync(MailHistoryQuery query);
    Task<MailHistoryDetail?> GetByIdAsync(int mailHistoryId);
    Task<List<string>> GetMailTypesAsync();
}
