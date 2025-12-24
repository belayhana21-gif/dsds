using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMT.Domain.Models;

[Table("users")]
public class User : BaseEntity
{
    [Key]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [StringLength(50)]
    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    [Column("password")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [Column("role")]
    public UserRole Role { get; set; }

    [Column("status")]
    public UserStatus Status { get; set; } = UserStatus.Active;

    [StringLength(255)]
    [Column("profile_picture_path")]
    public string? ProfilePicturePath { get; set; }

    [Column("supervisor_id")]
    public int? SupervisorId { get; set; }

    [Column("shop_id")]
    public int? ShopId { get; set; }

    [StringLength(255)]
    [Column("password_reset_token")]
    public string? PasswordResetToken { get; set; }

    [Column("password_reset_expires_at")]
    public DateTime? PasswordResetExpiresAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey("SupervisorId")]
    public virtual User? Supervisor { get; set; }

    [ForeignKey("ShopId")]
    public virtual Shop? Shop { get; set; }

    public virtual ICollection<Task> CreatedTasks { get; set; } = new List<Task>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public virtual PerformanceMetric? PerformanceMetric { get; set; }
}

public enum UserRole
{
    [Display(Name = "Team Leader")]
    TeamLeader,
    
    [Display(Name = "Director")]
    Director,
    
    [Display(Name = "Engineer")]
    Engineer,
    
    [Display(Name = "Customer Personnel")]
    CustomerPersonnel,
    
    [Display(Name = "Customer")]
    Customer,
    
    [Display(Name = "Shop Team Leader")]
    ShopTL
}

public enum UserStatus
{
    Active,
    Inactive,
    Suspended
}