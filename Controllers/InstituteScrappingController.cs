using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.InstituteScrapping;
using AvecADeskApi.Repositories.InstituteScrapping;
using AvecADeskApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace AvecADeskApi.Controllers;

[Route("api/institutes-scrapping")]
[ApiController]
[Authorize]
public class InstituteScrappingController : ControllerBase
{
    private readonly IInstituteScrappingRepository _repository;
    private readonly IInstituteScrappingService _scrappingService;
    private readonly LogHelper _logHelper;

    public InstituteScrappingController(
        IInstituteScrappingRepository repository,
        IInstituteScrappingService scrappingService,
        LogHelper logHelper)
    {
        _repository = repository;
        _scrappingService = scrappingService;
        _logHelper = logHelper;
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportExcel([FromQuery] string? instituteName)
    {
        try
        {
            var rows = await _repository.GetAllAsync(instituteName);
            if (rows.Count == 0)
                return NotFound("No institute scrapping records found for export.");

            var fileBytes = InstituteScrappingExcelExporter.BuildWorkbook(rows);
            var fileName = $"institute-scrap-list-{DateTime.UtcNow:yyyy-MM-dd}.xlsx";
            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(ExportExcel), ex);
            return StatusCode(500, "An error occurred while exporting institute scrapping records.");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? instituteName)
    {
        try
        {
            return Ok(await _repository.GetAllAsync(instituteName));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetAll), ex);
            return StatusCode(500, "An error occurred while fetching institute scrapping records.");
        }
    }
    [HttpGet("institutenames")]
    public async Task<IActionResult> GetUniqueInstituteNames()
    {
        try
        {
            var institutes = await _repository.GetUniqueInstituteNamesAsync();
            return Ok(institutes);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
                {message = "Error while fetching institute names.",  error = ex.Message});
        }
    }
    [HttpGet("{scrappingId:int}")]
    public async Task<IActionResult> GetById(int scrappingId)
    {
        try
        {
            var record = await _repository.GetByIdAsync(scrappingId);
            if (record == null)
                return NotFound("Institute scrapping record not found.");

            return Ok(record);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(GetById), ex);
            return StatusCode(500, "An error occurred while fetching the institute scrapping record.");
        }
    }

    [HttpPost("run")]
    public async Task<IActionResult> RunScrape([FromBody] InstituteScrappingRunRequest request)
    {
        try
        {
            if (request == null)
                return BadRequest("Request body is required.");

            var result = await _scrappingService.RunScrapeAsync(request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (TaskCanceledException)
        {
            return StatusCode(504, "Scraping timed out. The website is large — please try again.");
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(RunScrape), ex);
            var message = ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
                ? "Scraping timed out. Please try again."
                : $"Scraping failed: {ex.Message}";
            return StatusCode(500, message);
        }
    }

    [HttpPost("manual")]
    public async Task<IActionResult> ManualCreate([FromBody] InstituteScrappingUpsertRequest request)
    {
        try
        {
            if (request == null)
                return BadRequest("Request body is required.");

            if (string.IsNullOrWhiteSpace(request.InstituteName))
                return BadRequest("Institute name is required.");

            if (string.IsNullOrWhiteSpace(request.ProgramName))
                return BadRequest("Program name is required.");

            var result = await _repository.ManualCreateAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(ManualCreate), ex);
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] InstituteScrappingUpsertRequest request)
    {
        try
        {
            var scrappingId = await _repository.CreateAsync(request);
            return Ok(await _repository.GetByIdAsync(scrappingId));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(Create), ex);
            return StatusCode(500, "An error occurred while creating the institute scrapping record.");
        }
    }

    [HttpPut("{scrappingId:int}")]
    public async Task<IActionResult> Update(int scrappingId, [FromBody] InstituteScrappingUpsertRequest request)
    {
        try
        {
            if (!await _repository.UpdateAsync(scrappingId, request))
                return NotFound("Institute scrapping record not found.");

            return Ok(await _repository.GetByIdAsync(scrappingId));
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(Update), ex);
            return StatusCode(500, "An error occurred while updating the institute scrapping record.");
        }
    }

    [HttpDelete("{scrappingId:int}")]
    public async Task<IActionResult> Delete(int scrappingId)
    {
        try
        {
            if (!await _repository.DeleteAsync(scrappingId))
                return NotFound("Institute scrapping record not found.");

            return NoContent();
        }
        catch (SqlException ex) when (ex.Number is >= 50001 and <= 50099)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logHelper.LogError(nameof(Delete), ex);
            return StatusCode(500, "An error occurred while deleting the institute scrapping record.");
        }
    }
}
