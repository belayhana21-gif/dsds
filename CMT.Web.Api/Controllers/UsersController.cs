using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CMT.Application.DTOs;
using CMT.Application.Interfaces;
using CMT.Web.Api.Attributes;
using System.ComponentModel.DataAnnotations;

namespace CMT.Web.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly CMT.Application.Interfaces.IAuthorizationService _authorizationService;

        public UsersController(IUserService userService, CMT.Application.Interfaces.IAuthorizationService authorizationService)
        {
            _userService = userService;
            _authorizationService = authorizationService;
        }

        [HttpGet]
        [RequirePermission("view_all_users")]
        public async Task<ActionResult<List<UserDto>>> GetAllUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving users", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [RequirePermission("view_all_users")]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                    return NotFound(new { message = "User not found" });

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the user", error = ex.Message });
            }
        }

        [HttpGet("by-username/{username}")]
        [RequirePermission("view_all_users")]
        public async Task<ActionResult<UserDto>> GetUserByUsername(string username)
        {
            try
            {
                var user = await _userService.GetUserByUsernameAsync(username);
                if (user == null)
                    return NotFound(new { message = "User not found" });

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the user", error = ex.Message });
            }
        }

        [HttpGet("by-role/{role}")]
        [RequirePermission("view_all_users")]
        public async Task<ActionResult<List<UserDto>>> GetUsersByRole(string role)
        {
            try
            {
                var users = await _userService.GetUsersByRoleAsync(role);
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving users by role", error = ex.Message });
            }
        }

        [HttpGet("by-shop/{shopId}")]
        [RequirePermission("view_all_users")]
        public async Task<ActionResult<List<UserDto>>> GetUsersByShop(int shopId)
        {
            try
            {
                var users = await _userService.GetUsersByShopAsync(shopId);
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving users by shop", error = ex.Message });
            }
        }

        [HttpGet("subordinates/{supervisorId}")]
        [RequirePermission("view_all_users")]
        public async Task<ActionResult<List<UserDto>>> GetSubordinates(int supervisorId)
        {
            try
            {
                var users = await _userService.GetSubordinatesAsync(supervisorId);
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving subordinates", error = ex.Message });
            }
        }

        [HttpPost]
        [RequirePermission("create_user")]
        public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var user = await _userService.CreateUserAsync(createUserDto);
                return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, user);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the user", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [RequirePermission("update_user")]
        public async Task<ActionResult<UserDto>> UpdateUser(int id, [FromBody] UpdateUserDto updateUserDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var user = await _userService.UpdateUserAsync(id, updateUserDto);
                return Ok(user);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the user", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [RequirePermission("delete_user")]
        public async Task<ActionResult> DeleteUser(int id)
        {
            try
            {
                var result = await _userService.DeleteUserAsync(id);
                if (!result)
                    return NotFound(new { message = "User not found" });

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the user", error = ex.Message });
            }
        }

        [HttpPost("{id}/change-password")]
        public async Task<ActionResult> ChangePassword(int id, [FromBody] ChangePasswordDto changePasswordDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Users can only change their own password unless they have manage permissions
                var currentUserId = GetCurrentUserId();
                if (currentUserId != id && !await _authorizationService.HasPermissionAsync(User, "update_user"))
                    return Forbid();

                var result = await _userService.ChangePasswordAsync(id, changePasswordDto);
                if (!result)
                    return BadRequest(new { message = "Current password is incorrect or user not found" });

                return Ok(new { message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while changing the password", error = ex.Message });
            }
        }

        [HttpPost("{id}/reset-password")]
        [RequirePermission("update_user")]
        public async Task<ActionResult> ResetPassword(int id, [FromBody] ResetPasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _userService.ResetPasswordAsync(id, request.NewPassword);
                if (!result)
                    return NotFound(new { message = "User not found" });

                return Ok(new { message = "Password reset successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while resetting the password", error = ex.Message });
            }
        }

        [HttpGet("check-username/{username}")]
        public async Task<ActionResult<bool>> CheckUsernameAvailability(string username, [FromQuery] int? excludeUserId = null)
        {
            try
            {
                var isAvailable = await _userService.IsUsernameAvailableAsync(username, excludeUserId);
                return Ok(new { available = isAvailable });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while checking username availability", error = ex.Message });
            }
        }

        [HttpGet("check-email/{email}")]
        public async Task<ActionResult<bool>> CheckEmailAvailability(string email, [FromQuery] int? excludeUserId = null)
        {
            try
            {
                var isAvailable = await _userService.IsEmailAvailableAsync(email, excludeUserId);
                return Ok(new { available = isAvailable });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while checking email availability", error = ex.Message });
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim?.Value, out var userId) ? userId : 0;
        }
    }

    public class ResetPasswordRequest
    {
        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;
    }
}