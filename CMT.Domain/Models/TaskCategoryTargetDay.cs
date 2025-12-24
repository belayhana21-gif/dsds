using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMT.Domain.Models;

[Table("task_category_target_days")]
public class TaskCategoryTargetDay : BaseEntity
{
    [Key]
    [Column("target_id")]
    public int TargetId { get; set; }

    [Required]
    [Column("category_id")]
    public int CategoryId { get; set; }

    [Required]
    [Column("target_days")]
    public int TargetDays { get; set; } = 5;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey("CategoryId")]
    public virtual TaskCategory Category { get; set; } = null!;
}