using AvecADeskApi.Model.DuePayment;
using AvecADeskApi.Model.EmailTemplate;

namespace AvecADeskApi.Interfaces;

public interface IDuePaymentRepository
{
    Task<List<DuePaymentResponse>> GetDuePaymentsAsync();
}
