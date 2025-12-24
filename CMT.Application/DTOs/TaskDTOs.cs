using System.ComponentModel.DataAnnotations;
using DomainTaskStatus = CMT.Domain.Models.TaskStatus;

namespace CMT.Application.DTOs
{
    public class TaskDto
    {
        public int TaskId { get; set; }
        public string? SerialNumber { get; set; }
        public string? PartNumber { get; set; }
        public string? PoNumber { get; set; }
        public string Description { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int? SubTypeId { get; set; }
        public int? RequestTypeId { get; set; }
        public DomainTaskStatus Status { get; set; }
        public string? Comments { get; set; }
        public string AssignedEngineer { get; set; } = "Unassigned";
        public int PriorityId { get; set; }
        public string? PriorityName { get; set; }
        public DateTime EstimatedCompletionDate { get; set; }
        public DateTime? TargetCompletionDate { get; set; }
        public DateTime? ActualCompletionDate { get; set; }
        public string? AttachmentPath { get; set; }
        public int? AttachmentsCount { get; set; }
        public int? ShopId { get; set; }
        public string? ShopName { get; set; }
        public int CreatedBy { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CompletedTaskDto
    {
        public int CompletedTaskId { get; set; }
        public int OriginalTaskId { get; set; }
        public string? SerialNumber { get; set; }
        public string? PartNumber { get; set; }
        public string? PoNumber { get; set; }
        public string Description { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int? SubTypeId { get; set; }
        public int? RequestTypeId { get; set; }
        public DomainTaskStatus Status { get; set; }
        public string? Comments { get; set; }
        public string AssignedEngineer { get; set; } = "Unassigned";
        public int PriorityId { get; set; }
        public string? PriorityName { get; set; }
        public DateTime EstimatedCompletionDate { get; set; }
        public DateTime? TargetCompletionDate { get; set; }
        public DateTime? ActualCompletionDate { get; set; }
        public string? AttachmentPath { get; set; }
        public int? AttachmentsCount { get; set; }
        public int? ShopId { get; set; }
        public string? ShopName { get; set; }
        public int CreatedBy { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime TaskCreatedAt { get; set; }
        public DateTime TaskUpdatedAt { get; set; }
        public DateTime CompletedAt { get; set; }
        public DateTime MovedToCompletedAt { get; set; }
    }

    public class CreateTaskDto
    {
        [StringLength(100)]
        public string? SerialNumber { get; set; }

        [StringLength(100)]
        public string? PartNumber { get; set; }

        [StringLength(100)]
        public string? PoNumber { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public int CategoryId { get; set; }

        public int? SubTypeId { get; set; }
        public int? RequestTypeId { get; set; }
        public DomainTaskStatus Status { get; set; } = DomainTaskStatus.Pending;
        public string? Comments { get; set; }

        [StringLength(255)]
        public string AssignedEngineer { get; set; } = "Unassigned";

        [Required]
        public int PriorityId { get; set; }

        [Required]
        public DateTime EstimatedCompletionDate { get; set; }

        public DateTime? TargetCompletionDate { get; set; }
        public string? AttachmentPath { get; set; }
        public int? ShopId { get; set; }

        [Required]
        public int CreatedBy { get; set; }
    }

    public class UpdateTaskDto
    {
        [StringLength(100)]
        public string? SerialNumber { get; set; }

        [StringLength(100)]
        public string? PartNumber { get; set; }

        [StringLength(100)]
        public string? PoNumber { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public int CategoryId { get; set; }

        public int? SubTypeId { get; set; }
        public int? RequestTypeId { get; set; }
        public DomainTaskStatus Status { get; set; }
        public string? Comments { get; set; }

        [StringLength(255)]
        public string AssignedEngineer { get; set; } = "Unassigned";

        [Required]
        public int PriorityId { get; set; }

        [Required]
        public DateTime EstimatedCompletionDate { get; set; }

        public DateTime? TargetCompletionDate { get; set; }
        public DateTime? ActualCompletionDate { get; set; }
        public string? AttachmentPath { get; set; }
        public int? ShopId { get; set; }

        public string? RevisionNotes { get; set; }
        public int? ShowRevisionAlert { get; set; }
        public string? UpdateComment { get; set; }
        public object? _editor { get; set; }
    }

    public class TaskFilterDto
    {
        public int? ShopId { get; set; }
        public int? CreatedBy { get; set; }
        public DomainTaskStatus? Status { get; set; }
        public int? PriorityId { get; set; }
        public int? CategoryId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SearchQuery { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}