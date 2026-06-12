using AvecADeskApi.Model.Commission;

namespace AvecADeskApi.Interfaces;

public interface ICommissionRepository
{
    Task<List<CommissionRateResponse>> GetVendorCommissionRatesAsync(int vendorId);
    Task<int> SetVendorCommissionRateAsync(int vendorId, CommissionRateCreateRequest request);
    Task<List<CommissionRateResponse>> GetInstituteCommissionRatesAsync(int instituteId);
    Task<int> SetInstituteCommissionRateAsync(int instituteId, CommissionRateCreateRequest request);
    Task<List<CommissionEarningResponse>> GetCommissionEarningsAsync(int? vendorId);
    Task<CommissionForecastResponse?> GetCommissionEarningsForecastAsync(int? vendorId);
    Task<CommissionEarningResponse?> ApproveCommissionEarningAsync(int earningId, int? approvedByUserId);
    Task<List<CommissionEarningResponse>> GetCommissionStatementAsync(int vendorId);
}
