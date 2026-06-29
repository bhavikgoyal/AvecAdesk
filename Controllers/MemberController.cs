using System.Security.Claims;
using AvecADeskApi.Interfaces;
using AvecADeskApi.LOG;
using AvecADeskApi.Model.UserResponse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvecADeskApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MembersController : ControllerBase
    {
        private readonly IMembersRepository _membersRepository;

        public MembersController(IMembersRepository membersRepository)
        {
            _membersRepository = membersRepository;
        }

        [HttpGet("Users_List")]
        public async Task<ActionResult<List<UserResponse>>> GetAllUsers()
        {
            try
            {
                int loginUserId = 15;
                string roleName = "Super Admin";

                var users = await _membersRepository.GetAllUsersAsync(loginUserId, roleName);

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.ToString());
            }
        }

        [HttpPost("create")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateUser([FromBody] UserResponse request)
        {
            try
            {
                if (request == null)
                    return BadRequest("Invalid payload");

                var existingUser = await _membersRepository.GetUserByUserNameAsync(request.UserName);
                if (existingUser != null)
                {
                    return Conflict(new
                    {
                        Message = $"Username '{request.UserName}' already exists"
                    });
                }

                var userId = await _membersRepository.CreateUserAsync(request);

                return Ok(new
                {
                    Message = "User created successfully",
                    UserId = userId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.ToString());
            }
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateUser([FromBody] UserResponse request)
        {
            try
            {
                if (request == null || request.UserId <= 0)
                {
                    return BadRequest("Invalid payload");
                }

                var success = await _membersRepository.UpdateUserAsync(request);

                if (!success)
                {
                    return NotFound(new { Message = "User not found" });
                }

                return Ok(new
                {
                    Success = true,
                    Message = "User updated successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while processing the request.");
            }
        }

        [HttpDelete("delete/{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            try
            {
                if (userId <= 0)
                {
                    return BadRequest(new { Message = "Invalid user id" });
                }

                var success = await _membersRepository.DeleteUserAsync(userId);

                if (!success)
                {
                    return NotFound(new { Message = "User not found" });
                }

                return Ok(new
                {
                    Success = true,
                    Message = "User deactivated successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while processing the request.");
            }
        }

        [HttpPatch("Resign/{userId}")]
        public async Task<IActionResult> ResignMember(int userId)
        {
            if (userId <= 0)
                return BadRequest(new { Message = "Invalid user id" });

            try
            {
                var resignDate = DateTime.UtcNow;

 
                await _membersRepository.ResignMemberAsync(userId, resignDate);

                return Ok(new MemberResignResponse
                {
                    Success = true,
                    Message = "Member resigned successfully",
                    MemberResingOn = resignDate
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while processing the request." });
            }
        }
    }
}