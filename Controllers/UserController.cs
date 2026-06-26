using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.UserPassword;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AvecADeskApi.Controllers;

[Route("api/user")]
[ApiController]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly LogHelper _logHelper;

    public UserController(IUserRepository userRepository, LogHelper logHelper)
    {
        _userRepository = userRepository;
        _logHelper = logHelper;
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(UserChangePasswordRequest request)
    {
        try
        {
            await _userRepository.ChangePasswordAsync(request);
            return Ok(new
            {
                Success = true,
                Message = "Password changed successfully."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Success = false,
                Message = ex.Message
            });
        }
    }
}
