using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.VendorStudent;
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers
{
  [ApiController]
  [Route("api/[controller]")]
  public class VendorStudentController : ControllerBase
  {
    private readonly IVendorStudentRepository _repo;
    private readonly LogHelper _logHelper;
    private readonly IWebHostEnvironment _env;

    public VendorStudentController(
        IVendorStudentRepository repo,
        LogHelper logHelper,
        IWebHostEnvironment env)
    {
      _repo = repo;
      _logHelper = logHelper;
      _env = env;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateStudent([FromBody] SaveVendorStudentRequest request)
    {
      try
      {
        int studentId = await _repo.CreateVendorStudentAsync(request);
        return Ok(new VendorStudentResponse
        {
          StudentID = studentId,
          Message = "Student application created successfully."
        });
      }
      catch (Exception ex)
      {
        _logHelper.LogError($"{nameof(VendorStudentController)}.{nameof(CreateStudent)}", ex);
        return StatusCode(500, new { message = "Error while creating student application.", detail = ex.Message });
      }
    }

    [HttpPut("{studentId:int}/agent")]
    public async Task<IActionResult> SaveAgent(int studentId, [FromBody] UpdateVendorStudentAgentRequest request)
    {
      try
      {
        await _repo.UpdateAgentDetailsAsync(studentId, request);
        return Ok(new { Message = "Agent details saved successfully." });
      }
      catch (Exception ex)
      {
        _logHelper.LogError($"{nameof(VendorStudentController)}.{nameof(SaveAgent)}", ex);
        return StatusCode(500, new { message = "Error while saving agent details.", detail = ex.Message });
      }
    }

    [HttpPut("{studentId:int}/immigration")]
    public async Task<IActionResult> SaveImmigration(int studentId, [FromBody] UpdateVendorStudentImmigrationRequest request)
    {
      try
      {
        await _repo.UpdateImmigrationAsync(studentId, request);
        return Ok(new { Message = "Immigration details saved successfully." });
      }
      catch (Exception ex)
      {
        _logHelper.LogError($"{nameof(VendorStudentController)}.{nameof(SaveImmigration)}", ex);
        return StatusCode(500, new { message = "Error while saving immigration details.", detail = ex.Message });
      }
    }

    [HttpPut("{studentId:int}/english")]
    public async Task<IActionResult> SaveEnglish(int studentId, [FromBody] UpdateVendorStudentEnglishRequest request)
    {
      try
      {
        await _repo.UpdateEnglishAsync(studentId, request);
        return Ok(new { Message = "English details saved successfully." });
      }
      catch (Exception ex)
      {
        _logHelper.LogError($"{nameof(VendorStudentController)}.{nameof(SaveEnglish)}", ex);
        return StatusCode(500, new { message = "Error while saving English details.", detail = ex.Message });
      }
    }

    [HttpPost("{studentId:int}/education")]
    public async Task<IActionResult> SaveEducation(int studentId, [FromBody] SaveStudentEducationHistoryRequest request)
    {
      try
      {
        await _repo.SaveEducationHistoryAsync(studentId, request);
        return Ok(new { Message = "Education history saved successfully." });
      }
      catch (Exception ex)
      {
        _logHelper.LogError($"{nameof(VendorStudentController)}.{nameof(SaveEducation)}", ex);
        return StatusCode(500, new { message = "Error while saving education history.", detail = ex.Message });
      }
    }

    [HttpPost("{studentId:int}/documents")]
    public async Task<IActionResult> UploadDocument(int studentId, [FromForm] VendorStudentDocumentRequest request)
    {
      try
      {
        if (request.File == null || request.File.Length == 0)
          return BadRequest(new { Message = "File is required." });

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var uploadsFolder = Path.Combine(webRoot, "uploads", "vendor-student", studentId.ToString());
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid()}_{request.File.FileName}";
        var physicalPath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(physicalPath, FileMode.Create))
          await request.File.CopyToAsync(stream);

        var fileUrl = $"/uploads/vendor-student/{studentId}/{fileName}";
        var documentId = await _repo.SaveDocumentAsync(studentId, request.DocumentCategory, request.DocumentType, fileUrl);

        return Ok(new { DocumentID = documentId, FilePath = fileUrl, Message = "Document uploaded successfully." });
      }
      catch (Exception ex)
      {
        _logHelper.LogError($"{nameof(VendorStudentController)}.{nameof(UploadDocument)}", ex);
        return StatusCode(500, new { message = "Error while uploading document.", detail = ex.Message });
      }
    }

    [HttpPut("{studentId:int}/checklist")]
    public async Task<IActionResult> SaveChecklist(int studentId, [FromBody] UpdateVendorStudentChecklistRequest request)
    {
      try
      {
        await _repo.UpdateChecklistAsync(studentId, request);
        return Ok(new { Message = "Checklist saved successfully." });
      }
      catch (Exception ex)
      {
        _logHelper.LogError($"{nameof(VendorStudentController)}.{nameof(SaveChecklist)}", ex);
        return StatusCode(500, new { message = "Error while saving checklist.", detail = ex.Message });
      }
    }

    [HttpPut("{studentId:int}/declaration")]
    public async Task<IActionResult> SaveDeclaration(int studentId, [FromBody] UpdateVendorStudentDeclarationRequest request)
    {
      try
      {
        if (string.IsNullOrWhiteSpace(request.ApplicantSignaturePath))
          return BadRequest(new { Message = "Applicant signature is required." });

        var applicantSignaturePath = await SaveSignatureImageAsync(
            studentId,
            request.ApplicantSignaturePath,
            "applicant");
        if (string.IsNullOrWhiteSpace(applicantSignaturePath))
          return BadRequest(new { Message = "Invalid applicant signature. Please draw your signature again." });

        request.ApplicantSignaturePath = applicantSignaturePath;

        if (!string.IsNullOrWhiteSpace(request.ParentSignaturePath))
        {
          var parentSignaturePath = await SaveSignatureImageAsync(
              studentId,
              request.ParentSignaturePath,
              "parent");
          if (string.IsNullOrWhiteSpace(parentSignaturePath))
            return BadRequest(new { Message = "Invalid parent/guardian signature. Please draw the signature again." });

          request.ParentSignaturePath = parentSignaturePath;
        }

        await _repo.UpdateDeclarationAsync(studentId, request);
        return Ok(new
        {
          Message = "Declaration saved successfully.",
          ApplicantSignaturePath = request.ApplicantSignaturePath,
          ParentSignaturePath = request.ParentSignaturePath
        });
      }
      catch (Exception ex)
      {
        _logHelper.LogError($"{nameof(VendorStudentController)}.{nameof(SaveDeclaration)}", ex);
        return StatusCode(500, new { message = "Error while saving declaration.", detail = ex.Message });
      }
    }

    [HttpPost("{studentId:int}/submit")]
    public async Task<IActionResult> Submit(int studentId)
    {
      try
      {
        await _repo.SubmitAsync(studentId);
        return Ok(new { Message = "Application submitted successfully." });
      }
      catch (Exception ex)
      {
        _logHelper.LogError($"{nameof(VendorStudentController)}.{nameof(Submit)}", ex);
        return StatusCode(500, new { message = "Error while submitting application.", detail = ex.Message });
      }
    }

    [HttpGet("{studentId:int}")]
    public async Task<IActionResult> GetById(int studentId)
    {
      try
      {
        var result = await _repo.GetByIdAsync(studentId);
        if (result == null)
          return NotFound(new { Message = "Application not found." });

        return Ok(result);
      }
      catch (Exception ex)
      {
        _logHelper.LogError($"{nameof(VendorStudentController)}.{nameof(GetById)}", ex);
        return StatusCode(500, new { message = "Error while fetching application details.", detail = ex.Message });
      }
    }

    [HttpGet("vendor/{vendorId:int}/history")]
    public async Task<IActionResult> GetHistory(
        int vendorId,
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
      try
      {
        if (vendorId <= 0)
          return BadRequest(new { Message = "Valid VendorId is required." });

        var result = await _repo.GetHistoryAsync(vendorId, search, pageNumber, pageSize);
        var totalRecords = result.Count > 0 ? result[0].TotalRecords : 0;

        return Ok(new
        {
          Data = result,
          TotalRecords = totalRecords,
          PageNumber = pageNumber,
          PageSize = pageSize
        });
      }
      catch (Exception ex)
      {
        _logHelper.LogError($"{nameof(VendorStudentController)}.{nameof(GetHistory)}", ex);
        return StatusCode(500, new { message = "Error while fetching application history.", detail = ex.Message });
      }
    }
[HttpGet("GetStudentApplicationList")]
        public async Task<IActionResult> GetStudentApplicationList(
    [FromQuery] string? search,
    [FromQuery] int pagenumber = 1,
    [FromQuery] int pageSize = 200)
        {
            try
            {
                var result = await _repo.GetStudentApplicationListAsync(search, pagenumber, pageSize);
                var totalRecords = result.Count > 0 ? result[0].TotalRecords : 0;
 
                return Ok(new
                {
                    Data = result,
                    TotalRecords = totalRecords,
                    PageNumber = pagenumber,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"{nameof(VendorStudentController)}.{nameof(GetStudentApplicationList)}", ex);
                return StatusCode(500, new { message = "Error while fetching student applications.", detail = ex.Message });
            }
        }
    private async Task<string?> SaveSignatureImageAsync(int studentId, string signature, string prefix)
    {
      if (string.IsNullOrWhiteSpace(signature))
        return null;

      if (signature.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
      {
        var commaIndex = signature.IndexOf(',');
        if (commaIndex < 0)
          return null;

        byte[] bytes;
        try
        {
          bytes = Convert.FromBase64String(signature[(commaIndex + 1)..]);
        }
        catch (FormatException)
        {
          return null;
        }

        if (bytes.Length == 0)
          return null;

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var uploadsFolder = Path.Combine(webRoot, "uploads", "vendor-student", studentId.ToString(), "signatures");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{prefix}-signature-{Guid.NewGuid()}.png";
        var physicalPath = Path.Combine(uploadsFolder, fileName);
        await System.IO.File.WriteAllBytesAsync(physicalPath, bytes);

        return $"/uploads/vendor-student/{studentId}/signatures/{fileName}";
      }

      if (signature.Contains("/uploads/", StringComparison.OrdinalIgnoreCase))
        return signature.Trim();

      return null;
    }
  }
}
