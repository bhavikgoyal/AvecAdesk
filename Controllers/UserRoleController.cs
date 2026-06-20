using AvecADeskApi.Interfaces;
using AvecADeskApi.Model;
using AvecADeskApi.LOG;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserRoleController : ControllerBase
    {
        private readonly IUserRoleRepository _repo;
        private readonly ILogger<UserRoleController> _logger;

        public UserRoleController(IUserRoleRepository repo, ILogger<UserRoleController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                _logger.LogInformation("GetRoles called");
                var result = await _repo.GetRolesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetRoles failed");
                return StatusCode(500, ex.ToString());
            }
        }

        [HttpGet("companies")]
        public async Task<IActionResult> GetCompanies()
        {
            try
            {
                _logger.LogInformation("GetCompanies called");
                var result = await _repo.GetCompaniesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetCompanies failed");
                return StatusCode(500, ex.ToString());
            }
        }
    }
}
