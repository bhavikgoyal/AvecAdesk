using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.Commission;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;

namespace AvecADeskApi.Controllers;

[Route("api/commissions")]
[ApiController]
[Authorize]
public class CommissionsController : ControllerBase
{
    private readonly ICommissionRepository _commissionRepository;
    private readonly LogHelper _logHelper;

    private static readonly HashSet<string> AllowedRateTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Percentage", "Fixed"
    };

    public CommissionsController(ICommissionRepository commissionRepository, LogHelper logHelper)
    {
        _commissionRepository = commissionRepository;
        _logHelper = logHelper;
    }

    [HttpGet("rates")]
    public async Task<IActionResult> GetCommissionRates([FromQuery] int? vendorId)
    {
        try
        {
            return Ok(await _commissionRepository.GetCommissionRatesAsync(vendorId));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetCommissionRates), ex);
            return StatusCode(500, "An error occurred while fetching commission rates.");
        }
    }

    [HttpGet("vendor/{vendorId:int}")]
    public async Task<IActionResult> GetVendorCommissionRates(int vendorId)
    {
        try
        {
            return Ok(await _commissionRepository.GetVendorCommissionRatesAsync(vendorId));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetVendorCommissionRates), ex);
            return StatusCode(500, "An error occurred while fetching vendor commission rates.");
        }
    }

    [HttpPost("vendor/{vendorId:int}")]
    public async Task<IActionResult> SetVendorCommissionRate(int vendorId, [FromBody] CommissionRateCreateRequest request)
    {
        try
        {
            if (!AllowedRateTypes.Contains(request.RateType))
                return BadRequest("Invalid rate type. Use: Percentage or Fixed.");

            var commissionId = await _commissionRepository.SetVendorCommissionRateAsync(vendorId, request);
            var rates = await _commissionRepository.GetVendorCommissionRatesAsync(vendorId);
            var created = rates.FirstOrDefault(x => x.CommissionId == commissionId);
            return Ok(created ?? rates.LastOrDefault());
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(SetVendorCommissionRate), ex);
            return StatusCode(500, "An error occurred while setting vendor commission rate.");
        }
    }

    [HttpPut("vendor/{vendorId:int}/rates/{commissionId:int}")]
    public async Task<IActionResult> UpdateVendorCommissionRate(int vendorId, int commissionId, [FromBody] CommissionRateUpdateRequest request)
    {
        try
        {
            if (!AllowedRateTypes.Contains(request.RateType))
                return BadRequest("Invalid rate type. Use: Percentage or Fixed.");

            var updated = await _commissionRepository.UpdateVendorCommissionRateAsync(vendorId, commissionId, request);
            if (!updated)
                return NotFound("Commission rate not found.");

            var rates = await _commissionRepository.GetVendorCommissionRatesAsync(vendorId);
            var rate = rates.FirstOrDefault(x => x.CommissionId == commissionId);
            return Ok(rate);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(UpdateVendorCommissionRate), ex);
            return StatusCode(500, "An error occurred while updating vendor commission rate.");
        }
    }

    [HttpDelete("vendor/{vendorId:int}/rates/{commissionId:int}")]
    public async Task<IActionResult> DeleteVendorCommissionRate(int vendorId, int commissionId)
    {
        try
        {
            var deleted = await _commissionRepository.DeleteVendorCommissionRateAsync(vendorId, commissionId);
            if (!deleted)
                return NotFound("Commission rate not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(DeleteVendorCommissionRate), ex);
            return StatusCode(500, "An error occurred while deleting vendor commission rate.");
        }
    }

    [HttpGet("institute/{instituteId:int}")]
    public async Task<IActionResult> GetInstituteCommissionRates(int instituteId)
    {
        try
        {
            return Ok(await _commissionRepository.GetInstituteCommissionRatesAsync(instituteId));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetInstituteCommissionRates), ex);
            return StatusCode(500, "An error occurred while fetching institute commission rates.");
        }
    }

    [HttpPost("institute/{instituteId:int}")]
    public async Task<IActionResult> SetInstituteCommissionRate(int instituteId, [FromBody] CommissionRateCreateRequest request)
    {
        try
        {
            if (!AllowedRateTypes.Contains(request.RateType))
                return BadRequest("Invalid rate type. Use: Percentage or Fixed.");

            var commissionId = await _commissionRepository.SetInstituteCommissionRateAsync(instituteId, request);
            var rates = await _commissionRepository.GetInstituteCommissionRatesAsync(instituteId);
            var created = rates.FirstOrDefault(x => x.CommissionId == commissionId);
            return Ok(created ?? rates.LastOrDefault());
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(SetInstituteCommissionRate), ex);
            return StatusCode(500, "An error occurred while setting institute commission rate.");
        }
    }

    [HttpGet("earnings/forecast")]
    public async Task<IActionResult> GetCommissionEarningsForecast([FromQuery] int? vendorId)
    {
        try
        {
            var forecast = await _commissionRepository.GetCommissionEarningsForecastAsync(vendorId);
            return Ok(forecast ?? new CommissionForecastResponse());
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetCommissionEarningsForecast), ex);
            return StatusCode(500, "An error occurred while fetching commission forecast.");
        }
    }

    [HttpGet("earnings")]
    public async Task<IActionResult> GetCommissionEarnings([FromQuery] int? vendorId)
    {
        try
        {
            return Ok(await _commissionRepository.GetCommissionEarningsAsync(vendorId));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetCommissionEarnings), ex);
            return StatusCode(500, "An error occurred while fetching commission earnings.");
        }
    }

    [HttpPut("earnings/{earningId:int}/approve")]
    public async Task<IActionResult> ApproveCommissionEarning(int earningId)
    {
        try
        {
      var userId = 15;//GetCurrentUserId();
            if (userId == null)
                return Unauthorized("User ID not found in token. Please login again.");

            var earning = await _commissionRepository.ApproveCommissionEarningAsync(earningId, userId);
            if (earning == null)
                return NotFound("Earning not found or cannot be approved");

            return Ok(earning);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(ApproveCommissionEarning), ex);
            return StatusCode(500, "An error occurred while approving commission earning.");
        }
    }

    [HttpGet("statement/{vendorId:int}/pdf")]
    public async Task<IActionResult> GetCommissionStatementPdf(int vendorId)
    {
        try
        {
            var earnings = await _commissionRepository.GetCommissionStatementAsync(vendorId);
            var content = BuildStatementText(vendorId, earnings);
            var bytes = Encoding.UTF8.GetBytes(content);
            return File(bytes, "text/plain", $"commission-statement-{vendorId}.txt");
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetCommissionStatementPdf), ex);
            return StatusCode(500, "An error occurred while generating commission statement.");
        }
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (claim != null && int.TryParse(claim.Value, out var userId))
            return userId;

        return null;
    }

    private static string BuildStatementText(int vendorId, List<CommissionEarningResponse> earnings)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Commission Statement - Vendor {vendorId}");
        sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine(new string('-', 60));

        decimal total = 0;
        foreach (var e in earnings)
        {
            sb.AppendLine($"EarningId: {e.EarningId} | Amount: {e.EarnedAmount} | Status: {e.Status}");
            total += e.EarnedAmount;
        }

        sb.AppendLine(new string('-', 60));
        sb.AppendLine($"Total: {total}");
        return sb.ToString();
    }
}
