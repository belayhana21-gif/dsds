using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMT.Domain.Models;

[Table("task_comments")]
public class TaskComment : BaseEntity
{
    [Key]
    [Column("comment_id")]
    public int CommentId { get; set; }

    [Required]
    [Column("task_id")]
    public int TaskId { get; set; }

    [Required]
    [Column("author_id")]
    public int AuthorId { get; set; }

    [Required]
    [Column("content", TypeName = "TEXT")]
    public string Content { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey("TaskId")]
    public virtual Task Task { get; set; } = null!;

    [ForeignKey("AuthorId")]
    public virtual User Author { get; set; } = null!;
}