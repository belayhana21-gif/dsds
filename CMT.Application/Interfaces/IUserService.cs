using CMT.Application.DTOs;
using System.Security.Claims;

namespace CMT.Application.Interfaces
{
    public interface IUserService
    {
        Task<LoginResponseDto?> AuthenticateAsync(LoginDto loginDto);
        Task<LoginResponseDto?> RefreshTokenAsync(string refreshToken);
        Task<bool> RevokeTokenAsync(string token);
        Task<UserDto?> GetUserByIdAsync(int userId);
        Task<UserDto?> GetUserByUsernameAsync(string username);
        Task<List<UserDto>> GetAllUsersAsync();
        Task<List<UserDto>> GetUsersByRoleAsync(string role); // Changed from enum to string
        Task<List<UserDto>> GetUsersByShopAsync(int shopId);
        Task<List<UserDto>> GetSubordinatesAsync(int supervisorId);
        Task<UserDto> CreateUserAsync(CreateUserDto createUserDto);
        Task<UserDto> UpdateUserAsync(int userId, UpdateUserDto updateUserDto);
        Task<bool> DeleteUserAsync(int userId);
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto);
        Task<bool> ResetPasswordAsync(int userId, string newPassword);
        Task<bool> IsUsernameAvailableAsync(string username, int? excludeUserId = null);
        Task<bool> IsEmailAvailableAsync(string email, int? excludeUserId = null);
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
    }
}