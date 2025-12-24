using System.ComponentModel.DataAnnotations;

namespace CMT.Application.DTOs
{
    public enum UserRole
    {
        Customer = 1,
        CustomerPersonnel = 2,
        Engineer = 3,
        ShopTeamLeader = 4,
        TeamLeader = 5,
        Director = 6
    }

    public class UserDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // Changed from UserRole enum to string
        public string RoleDisplayName => GetRoleDisplayName(Role);
        public int? ShopId { get; set; }
        public string? ShopName { get; set; }
        public int? SupervisorId { get; set; }
        public string? SupervisorName { get; set; }
        public string? ProfilePicturePath { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; }
        public List<string> Permissions { get; set; } = new List<string>();

        private static string GetRoleDisplayName(string role)
        {
            return role switch
            {
                "team_leader" => "Team Leader",
                "director" => "Director",
                "engineer" => "Engineer",
                "customer_personnel" => "Customer Personnel",
                "customer" => "Customer",
                "shop_tl" => "Shop Team Leader",
                _ => role ?? "Unknown"
            };
        }
    }

    public class CreateUserDto
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty; // Changed from UserRole enum to string

        public int? ShopId { get; set; }

        public int? SupervisorId { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class UpdateUserDto
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        public string? Role { get; set; } // Changed from UserRole enum to string

        public int? ShopId { get; set; }

        public int? SupervisorId { get; set; }

        public bool? IsActive { get; set; }

        [MinLength(6)]
        public string? NewPassword { get; set; }
    }

    public class LoginDto
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public UserDto User { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public List<string> Permissions { get; set; } = new List<string>();
    }

    public class RefreshTokenDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class ChangePasswordDto
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare("NewPassword")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ShopDto
    {
        public int ShopId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Location { get; set; }
        public string? Code { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int UserCount { get; set; }
    }

    // Helper class for role validation
    public static class ValidRoles
    {
        public static readonly string[] AllRoles = {
            "team_leader",
            "director", 
            "engineer",
            "customer_personnel",
            "customer",
            "shop_tl"
        };

        public static bool IsValidRole(string role)
        {
            return AllRoles.Contains(role);
        }
    }
}