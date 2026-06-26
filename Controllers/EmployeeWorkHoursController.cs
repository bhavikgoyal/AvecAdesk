using AvecADeskApi.IRepository;
using AvecADeskApi.Model.EmployeeWorkHours;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AvecADeskApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmployeeWorkHoursController : ControllerBase
    {
        private readonly IEmployeeWorkHoursRepository _repo;

        private readonly ILogger<EmployeeWorkHoursController> _logger;

        public EmployeeWorkHoursController(
            IEmployeeWorkHoursRepository repo,
            ILogger<EmployeeWorkHoursController> logger)
        {
            _repo = repo;
            _logger = logger;
        }



        [HttpPost("Create Employee Work Hours")]
        public async Task<IActionResult> Insert([FromBody] StartStop model)
        {
            if (model == null)
                return BadRequest("Invalid request");

            try
            {
                var id = await _repo.InsertAsync(model);
                return Ok(new { Id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting StartStop");
                return StatusCode(500, "An error occurred while inserting the record.");
            }
        }


        [HttpPut("Update Employee Work Hours")]
        public async Task<IActionResult> Update([FromBody] StartStop dto)
        {
            try
            {
                var model = new StartStop
                {
                    Id = dto.Id,
                    Userid = dto.Userid,
                    StopTime = dto.StopTime,
                    MarkDone = dto.MarkDone
                };

                await _repo.UpdateAsync(model);

                return Ok(new { message = "Employee Work Hours updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating StartStop with Id {Id}", dto.Id);
                return StatusCode(500, "An error occurred while updating the record.");
            }
        }


        [HttpGet("Select_By_UserId")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _repo.GetAllByUserIdAsync(id);

                if (result == null)
                    return NotFound();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        [HttpGet("Employee Work Hours ALL")]
        public async Task<ActionResult<List<StartStop>>> GetAll()

        {
            try
            {
                var startStops = await _repo.GetAllByUserGetallAsync();
                return Ok(startStops);
            }
            catch (Exception ex)
            {

                return StatusCode(500, "An error occurred while retrieving Employee Work Hours records.");
            }
        }
    }
}

