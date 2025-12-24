using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMT.Domain.Models;

[Table("completed_tasks")]
public class CompletedTask : BaseEntity
{
    [Key]
    [Column("completed_task_id")]
    public int CompletedTaskId { get; set; }

    [Column("original_task_id")]
    public int OriginalTaskId { get; set; }

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
    public TaskStatus Status { get; set; } = TaskStatus.Completed;

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

    [Column("task_created_at")]
    public DateTime TaskCreatedAt { get; set; }

    [Column("task_updated_at")]
    public DateTime TaskUpdatedAt { get; set; }

    [Column("completed_at")]
    public DateTime CompletedAt { get; set; }

    [Column("moved_to_completed_at")]
    public DateTime MovedToCompletedAt { get; set; }

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

    public virtual ICollection<CompletedTaskComment> TaskComments { get; set; } = new List<CompletedTaskComment>();
    public virtual ICollection<CompletedTaskAttachment> Attachments { get; set; } = new List<CompletedTaskAttachment>();
}

[Table("completed_task_comments")]
public class CompletedTaskComment : BaseEntity
{
    [Key]
    [Column("comment_id")]
    public int CommentId { get; set; }

    [Required]
    [Column("completed_task_id")]
    public int CompletedTaskId { get; set; }

    [Required]
    [Column("author_id")]
    public int AuthorId { get; set; }

    [Required]
    [Column("comment_text", TypeName = "TEXT")]
    public string CommentText { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("CompletedTaskId")]
    public virtual CompletedTask CompletedTask { get; set; } = null!;

    [ForeignKey("AuthorId")]
    public virtual User Author { get; set; } = null!;
}

[Table("completed_task_attachments")]
public class CompletedTaskAttachment : BaseEntity
{
    [Key]
    [Column("attachment_id")]
    public int AttachmentId { get; set; }

    [Required]
    [Column("completed_task_id")]
    public int CompletedTaskId { get; set; }

    [Required]
    [StringLength(255)]
    [Column("file_name")]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    [Column("file_path")]
    public string FilePath { get; set; } = string.Empty;

    [StringLength(100)]
    [Column("file_type")]
    public string? FileType { get; set; }

    [Column("file_size")]
    public long? FileSize { get; set; }

    [Column("uploaded_at")]
    public DateTime UploadedAt { get; set; }

    [ForeignKey("CompletedTaskId")]
    public virtual CompletedTask CompletedTask { get; set; } = null!;
}