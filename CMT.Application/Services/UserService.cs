using CMT.Application.DTOs;
using CMT.Application.Interfaces;
using CMT.Domain.Models;
using CMT.Domain.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CMT.Application.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IAuthorizationService _authorizationService;
        private readonly IPasswordService _passwordService;

        public UserService(ApplicationDbContext context, IConfiguration configuration, IAuthorizationService authorizationService, IPasswordService passwordService)
        {
            _context = context;
            _configuration = configuration;
            _authorizationService = authorizationService;
            _passwordService = passwordService;
        }

        public async System.Threading.Tasks.Task<LoginResponseDto?> AuthenticateAsync(LoginDto loginDto)
        {
            Console.WriteLine($"[DEBUG] Attempting login for username: {loginDto.Username}");
            
            var user = await _context.Users
                .Include(u => u.Shop)
                .Include(u => u.Supervisor)
                .FirstOrDefaultAsync(u => u.Username == loginDto.Username && u.Status == UserStatus.Active);

            if (user == null)
            {
                Console.WriteLine($"[DEBUG] User not found or inactive: {loginDto.Username}");
                return null;
            }

            Console.WriteLine($"[DEBUG] User found: {user.Username}, Role: {user.Role}");
            Console.WriteLine($"[DEBUG] Stored password hash: {user.Password}");
            Console.WriteLine($"[DEBUG] Input password hash: {HashPassword(loginDto.Password)}");

            if (!VerifyPassword(loginDto.Password, user.Password))
            {
                Console.WriteLine($"[DEBUG] Password verification failed for user: {user.Username}");
                return null;
            }

            Console.WriteLine($"[DEBUG] Password verification successful for user: {user.Username}");

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            // Update last login
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var permissions = await _authorizationService.GetUserPermissionsAsync(user.UserId);

            return new LoginResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                User = MapToUserDto(user),
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                Permissions = permissions
            };
        }

        public System.Threading.Tasks.Task<LoginResponseDto?> RefreshTokenAsync(string refreshToken)
        {
            // For now, return null - implement session management later
            return System.Threading.Tasks.Task.FromResult<LoginResponseDto?>(null);
        }

        public System.Threading.Tasks.Task<bool> RevokeTokenAsync(string token)
        {
            // For now, return true - implement session management later
            return System.Threading.Tasks.Task.FromResult(true);
        }

        public async System.Threading.Tasks.Task<UserDto?> GetUserByIdAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.Shop)
                .Include(u => u.Supervisor)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            return user != null ? MapToUserDto(user) : null;
        }

        public async System.Threading.Tasks.Task<UserDto?> GetUserByUsernameAsync(string username)
        {
            var user = await _context.Users
                .Include(u => u.Shop)
                .Include(u => u.Supervisor)
                .FirstOrDefaultAsync(u => u.Username == username);

            return user != null ? MapToUserDto(user) : null;
        }

        public async System.Threading.Tasks.Task<List<UserDto>> GetAllUsersAsync()
        {
            var users = await _context.Users
                .Include(u => u.Shop)
                .Include(u => u.Supervisor)
                .Where(u => u.Status == UserStatus.Active)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return users.Select(MapToUserDto).ToList();
        }

        public async System.Threading.Tasks.Task<List<UserDto>> GetUsersByRoleAsync(string role)
        {
            // Convert string role to domain role enum
            var domainRole = ConvertStringToUserRole(role);
            var users = await _context.Users
                .Include(u => u.Shop)
                .Include(u => u.Supervisor)
                .Where(u => u.Role == domainRole && u.Status == UserStatus.Active)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return users.Select(MapToUserDto).ToList();
        }

        public async System.Threading.Tasks.Task<List<UserDto>> GetUsersByShopAsync(int shopId)
        {
            var users = await _context.Users
                .Include(u => u.Shop)
                .Include(u => u.Supervisor)
                .Where(u => u.ShopId == shopId && u.Status == UserStatus.Active)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return users.Select(MapToUserDto).ToList();
        }

        public async System.Threading.Tasks.Task<List<UserDto>> GetSubordinatesAsync(int supervisorId)
        {
            var users = await _context.Users
                .Include(u => u.Shop)
                .Include(u => u.Supervisor)
                .Where(u => u.SupervisorId == supervisorId && u.Status == UserStatus.Active)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return users.Select(MapToUserDto).ToList();
        }

        public async System.Threading.Tasks.Task<UserDto> CreateUserAsync(CreateUserDto createUserDto)
        {
            if (!await IsUsernameAvailableAsync(createUserDto.Username))
                throw new InvalidOperationException("Username already exists");

            if (!await IsEmailAvailableAsync(createUserDto.Email))
                throw new InvalidOperationException("Email already exists");

            // Validate role
            if (!ValidRoles.IsValidRole(createUserDto.Role))
                throw new InvalidOperationException($"Invalid role: {createUserDto.Role}");

            var user = new User
            {
                FullName = createUserDto.FullName,
                Username = createUserDto.Username,
                Email = createUserDto.Email,
                Password = HashPassword(createUserDto.Password),
                Role = ConvertStringToUserRole(createUserDto.Role),
                ShopId = createUserDto.ShopId,
                SupervisorId = createUserDto.SupervisorId,
                Status = createUserDto.IsActive ? UserStatus.Active : UserStatus.Inactive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return await GetUserByIdAsync(user.UserId) ?? throw new InvalidOperationException("Failed to create user");
        }

        public async System.Threading.Tasks.Task<UserDto> UpdateUserAsync(int userId, UpdateUserDto updateUserDto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new InvalidOperationException("User not found");

            if (!await IsEmailAvailableAsync(updateUserDto.Email, userId))
                throw new InvalidOperationException("Email already exists");

            user.FullName = updateUserDto.FullName;
            user.Email = updateUserDto.Email;
            
            if (!string.IsNullOrEmpty(updateUserDto.Role))
            {
                if (!ValidRoles.IsValidRole(updateUserDto.Role))
                    throw new InvalidOperationException($"Invalid role: {updateUserDto.Role}");
                user.Role = ConvertStringToUserRole(updateUserDto.Role);
            }
            
            if (updateUserDto.ShopId.HasValue)
                user.ShopId = updateUserDto.ShopId.Value;
            
            if (updateUserDto.SupervisorId.HasValue)
                user.SupervisorId = updateUserDto.SupervisorId.Value;
            
            if (updateUserDto.IsActive.HasValue)
                user.Status = updateUserDto.IsActive.Value ? UserStatus.Active : UserStatus.Inactive;

            if (!string.IsNullOrEmpty(updateUserDto.NewPassword))
                user.Password = HashPassword(updateUserDto.NewPassword);

            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetUserByIdAsync(userId) ?? throw new InvalidOperationException("Failed to update user");
        }

        public async System.Threading.Tasks.Task<bool> DeleteUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            user.Status = UserStatus.Inactive;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async System.Threading.Tasks.Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || !VerifyPassword(changePasswordDto.CurrentPassword, user.Password))
                return false;

            user.Password = HashPassword(changePasswordDto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async System.Threading.Tasks.Task<bool> ResetPasswordAsync(int userId, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            user.Password = HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async System.Threading.Tasks.Task<bool> IsUsernameAvailableAsync(string username, int? excludeUserId = null)
        {
            var query = _context.Users.Where(u => u.Username == username);
            
            if (excludeUserId.HasValue)
                query = query.Where(u => u.UserId != excludeUserId.Value);

            return !await query.AnyAsync();
        }

        public async System.Threading.Tasks.Task<bool> IsEmailAvailableAsync(string email, int? excludeUserId = null)
        {
            var query = _context.Users.Where(u => u.Email == email);
            
            if (excludeUserId.HasValue)
                query = query.Where(u => u.UserId != excludeUserId.Value);

            return !await query.AnyAsync();
        }

        public string HashPassword(string password)
        {
            return _passwordService.HashPassword(password);
        }

        public bool VerifyPassword(string password, string hash)
        {
            return _passwordService.VerifyPassword(password, hash);
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? "DefaultSecretKeyForDevelopment";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("FullName", user.FullName),
                new Claim("Role", ConvertUserRoleToString(user.Role)),
                new Claim("ShopId", user.ShopId?.ToString() ?? ""),
                new Claim("SupervisorId", user.SupervisorId?.ToString() ?? "")
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"] ?? "CMT",
                audience: jwtSettings["Audience"] ?? "CMT",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        private UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Username = user.Username,
                Email = user.Email,
                Role = ConvertUserRoleToString(user.Role),
                ShopId = user.ShopId,
                ShopName = user.Shop?.Name,
                SupervisorId = user.SupervisorId,
                SupervisorName = user.Supervisor?.FullName,
                ProfilePicturePath = user.ProfilePicturePath,
                CreatedAt = user.CreatedAt,
                LastLoginAt = null, // Add LastLoginAt property to User model if needed
                IsActive = user.Status == UserStatus.Active
            };
        }

        // Helper methods to convert between string and enum roles
        private CMT.Domain.Models.UserRole ConvertStringToUserRole(string role)
        {
            return role switch
            {
                "team_leader" => CMT.Domain.Models.UserRole.TeamLeader,
                "director" => CMT.Domain.Models.UserRole.Director,
                "engineer" => CMT.Domain.Models.UserRole.Engineer,
                "customer_personnel" => CMT.Domain.Models.UserRole.CustomerPersonnel,
                "customer" => CMT.Domain.Models.UserRole.Customer,
                "shop_tl" => CMT.Domain.Models.UserRole.ShopTL,
                _ => throw new ArgumentException($"Invalid role: {role}")
            };
        }

        private string ConvertUserRoleToString(CMT.Domain.Models.UserRole role)
        {
            return role switch
            {
                CMT.Domain.Models.UserRole.TeamLeader => "team_leader",
                CMT.Domain.Models.UserRole.Director => "director",
                CMT.Domain.Models.UserRole.Engineer => "engineer",
                CMT.Domain.Models.UserRole.CustomerPersonnel => "customer_personnel",
                CMT.Domain.Models.UserRole.Customer => "customer",
                CMT.Domain.Models.UserRole.ShopTL => "shop_tl",
                _ => "unknown"
            };
        }
    }
}