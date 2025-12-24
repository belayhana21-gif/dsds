using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMT.Domain.Models;

[Table("notifications")]
public class Notification : BaseEntity
{
    [Key]
    [Column("notification_id")]
    public int NotificationId { get; set; }

    [Required]
    [Column("recipient_id")]
    public int RecipientId { get; set; }

    [Required]
    [Column("message", TypeName = "TEXT")]
    public string Message { get; set; } = string.Empty;

    [Column("is_read")]
    public bool IsRead { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    [ForeignKey("RecipientId")]
    public virtual User Recipient { get; set; } = null!;
}