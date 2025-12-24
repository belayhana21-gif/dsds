using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMT.Domain.Models;

[Table("tasks")]
public class Task : BaseEntity
{
    [Key]
    [Column("task_id")]
    public int TaskId { get; set; }

    [StringLength(100)]
    [Column("serial_number")]
    public string? SerialNumber { get; set; }

    [StringLength(100)]
    [Column("part_number")]
    public string? PartNumber { get; set; }

    [StringLength(100)]
    [Column("po_number")]
    public string? PoNumber { get; set; }

    [Required]
    [Column("description", TypeName = "TEXT")]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Column("category_id")]
    public int CategoryId { get; set; }

    [Column("sub_type_id")]
    public int? SubTypeId { get; set; }

    [Column("request_type_id")]
    public int? RequestTypeId { get; set; }

    [Column("status")]
    public TaskStatus Status { get; set; } = TaskStatus.Pending;

    [Column("comments", TypeName = "TEXT")]
    public string? Comments { get; set; }

    [StringLength(255)]
    [Column("assigned_engineer")]
    public string AssignedEngineer { get; set; } = "Unassigned";

    [Required]
    [Column("priority_id")]
    public int PriorityId { get; set; }

    [Required]
    [Column("estimated_completion_date")]
    public DateTime EstimatedCompletionDate { get; set; }

    [Column("target_completion_date")]
    public DateTime? TargetCompletionDate { get; set; }

    [Column("actual_completion_date")]
    public DateTime? ActualCompletionDate { get; set; }

    [StringLength(255)]
    [Column("attachment_path")]
    public string? AttachmentPath { get; set; }

    [Column("amendment_request")]
    public bool AmendmentRequest { get; set; } = false;

    [Column("amendment_status")]
    public AmendmentStatus? AmendmentStatus { get; set; }

    [Column("amendment_reviewed_by_tl_id")]
    public int? AmendmentReviewedByTlId { get; set; }

    [Column("is_duplicate")]
    public bool IsDuplicate { get; set; } = false;

    [Column("duplicate_justification", TypeName = "TEXT")]
    public string? DuplicateJustification { get; set; }

    [Column("revision_notes", TypeName = "TEXT")]
    public string? RevisionNotes { get; set; }

    [Column("show_revision_alert")]
    public bool ShowRevisionAlert { get; set; } = false;

    [Column("shop_id")]
    public int? ShopId { get; set; }

    [Required]
    [Column("created_by")]
    public int CreatedBy { get; set; }

    [Column("is_mandatory")]
    public bool IsMandatory { get; set; } = false;

    [Column("cancelled_by")]
    public int? CancelledBy { get; set; }

    [Column("cancellation_reason", TypeName = "TEXT")]
    public string? CancellationReason { get; set; }

    [Column("cancelled_at")]
    public DateTime? CancelledAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey("CategoryId")]
    public virtual TaskCategory Category { get; set; } = null!;

    [ForeignKey("SubTypeId")]
    public virtual TaskSubType? SubType { get; set; }

    [ForeignKey("RequestTypeId")]
    public virtual RequestType? RequestType { get; set; }

    [ForeignKey("PriorityId")]
    public virtual TaskPriorityLevel Priority { get; set; } = null!;

    [ForeignKey("CreatedBy")]
    public virtual User Creator { get; set; } = null!;

    [ForeignKey("AmendmentReviewedByTlId")]
    public virtual User? AmendmentReviewer { get; set; }

    [ForeignKey("ShopId")]
    public virtual Shop? Shop { get; set; }

    [ForeignKey("CancelledBy")]
    public virtual User? CancelledByUser { get; set; }

    public virtual ICollection<TaskComment> TaskComments { get; set; } = new List<TaskComment>();
    public virtual ICollection<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();
}

public enum TaskStatus
{
    Pending,
    InProgress,
    Completed,
    Blocked,
    OnHold,
    Cancelled
}

public enum AmendmentStatus
{
    PendingTLReview,
    ForwardedToDirector,
    Approved,
    Rejected
}