using AvecADeskApi.Interfaces;
using AvecADeskApi.Model.Student;
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentApplicationController : ControllerBase
{
    private readonly IStudentApplicationRepository _repo;
    private readonly IWebHostEnvironment _env;

    public StudentApplicationController(IStudentApplicationRepository repo, IWebHostEnvironment env)
    {
        _repo = repo;
        _env = env;
    }

    // GET: api/StudentApplication/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _repo.GetApplicationByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    // POST: api/StudentApplication
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] StudentApplicationCreateRequest request)
    {
        var applicationId = await _repo.CreateApplicationAsync(request);
        return Ok(new { ApplicationId = applicationId });
    }

    // PUT: api/StudentApplication/{id}/details
    [HttpPut("{id}/details")]
    public async Task<IActionResult> SaveDetails(Guid id, [FromBody] ApplicationDetailRequest request)
    {
        var result = await _repo.SaveApplicationDetailAsync(id, request);
        if (!result) return NotFound();
        return Ok(new { Message = "Details saved successfully" });
    }


    // POST: api/StudentApplication/{id}/documents
    [HttpPost("{id}/documents")]
    public async Task<IActionResult> UploadDocument(Guid id, [FromForm] ApplicationDocumentRequest request)
    {
        if (request.File == null || request.File.Length == 0)
            return BadRequest(new { Message = "File is required" });

       
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var uploadsFolder = Path.Combine(webRoot, "uploads", id.ToString());
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid()}_{request.File.FileName}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
            await request.File.CopyToAsync(stream);

        var fileUrl = $"/uploads/{id}/{fileName}";
        var result = await _repo.UploadDocumentAsync(id, request, fileUrl);
        return Ok(result);
    }

    // DELETE: api/StudentApplication/documents/{docId}
    [HttpDelete("documents/{docId}")]
    public async Task<IActionResult> DeleteDocument(Guid docId)
    {
        var result = await _repo.DeleteDocumentAsync(docId);
        if (!result) return NotFound();
        return Ok(new { Message = "Document deleted successfully" });
    }

    // PUT: api/StudentApplication/{id}/declaration
    [HttpPut("{id}/declaration")]
    public async Task<IActionResult> SaveDeclaration(Guid id, [FromBody] DeclarationRequest request)
    {
        var result = await _repo.SaveDeclarationAsync(id, request);
        if (!result) return NotFound();
        return Ok(new { Message = "Declaration saved successfully" });
    }

    // POST: api/StudentApplication/{id}/submit
    [HttpPost("{id}/submit")]
    public async Task<IActionResult> Submit(Guid id)
    {
        var result = await _repo.SubmitApplicationAsync(id);
        if (!result) return NotFound();
        return Ok(new { Message = "Application submitted successfully" });
    }
}