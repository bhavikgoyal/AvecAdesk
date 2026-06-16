using AvecADeskApi.Model.Commission;

namespace AvecADeskApi.Interfaces;

public interface ICommissionRepository
{
    Task<List<CommissionRateResponse>> GetCommissionRatesAsync(int? vendorId = null);
    Task<List<CommissionRateResponse>> GetVendorCommissionRatesAsync(int vendorId);
    Task<int> SetVendorCommissionRateAsync(int vendorId, CommissionRateCreateRequest request);
    Task<bool> UpdateVendorCommissionRateAsync(int vendorId, int commissionId, CommissionRateUpdateRequest request);
    Task<bool> DeleteVendorCommissionRateAsync(int vendorId, int commissionId);
    Task<List<CommissionRateResponse>> GetInstituteCommissionRatesAsync(int instituteId);
    Task<int> SetInstituteCommissionRateAsync(int instituteId, CommissionRateCreateRequest request);
    Task<List<CommissionEarningResponse>> GetCommissionEarningsAsync(int? vendorId);
    Task<CommissionForecastResponse?> GetCommissionEarningsForecastAsync(int? vendorId);
    Task<CommissionEarningResponse?> ApproveCommissionEarningAsync(int earningId, int? approvedByUserId);
    Task<List<CommissionEarningResponse>> GetCommissionStatementAsync(int vendorId);
}
