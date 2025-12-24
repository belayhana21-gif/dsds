using CMT.Application.Repositories;
using CMT.Domain.Data;
using CMT.Domain.Models;
using Microsoft.EntityFrameworkCore;
using TaskEntity = CMT.Domain.Models.Task;
using TaskStatusEnum = CMT.Domain.Models.TaskStatus;

namespace CMT.Application.Services;

public interface ICompletedTaskService
{
    System.Threading.Tasks.Task<IEnumerable<CompletedTask>> GetAllCompletedTasksAsync();
    System.Threading.Tasks.Task<CompletedTask?> GetCompletedTaskByIdAsync(int completedTaskId);
    System.Threading.Tasks.Task<CompletedTask?> GetCompletedTaskByOriginalIdAsync(int originalTaskId);
    System.Threading.Tasks.Task<(IEnumerable<CompletedTask> tasks, int totalCount)> GetCompletedTasksWithFiltersAsync(
        int? categoryId = null,
        string? searchQuery = null,
        int? priorityId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageNumber = 1,
        int pageSize = 50);
    System.Threading.Tasks.Task<CompletedTask> MoveTaskToCompletedAsync(TaskEntity task);
    System.Threading.Tasks.Task<TaskEntity?> ReopenCompletedTaskAsync(int completedTaskId);
    System.Threading.Tasks.Task<bool> DeleteCompletedTaskAsync(int completedTaskId);
}

public class CompletedTaskService : ICompletedTaskService
{
    private readonly ApplicationDbContext _context;

    public CompletedTaskService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async System.Threading.Tasks.Task<IEnumerable<CompletedTask>> GetAllCompletedTasksAsync()
    {
        return await _context.CompletedTasks
            .Include(t => t.Creator)
            .Include(t => t.Shop)
            .Include(t => t.Category)
            .Include(t => t.Priority)
            .Include(t => t.Attachments)
            .Where(t => t.RecordStatus == RecordStatus.Active)
            .OrderByDescending(t => t.CompletedAt)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<CompletedTask?> GetCompletedTaskByIdAsync(int completedTaskId)
    {
        return await _context.CompletedTasks
            .Include(t => t.Creator)
            .Include(t => t.Shop)
            .Include(t => t.Category)
            .Include(t => t.Priority)
            .Include(t => t.Attachments)
            .Include(t => t.TaskComments)
            .FirstOrDefaultAsync(t => t.CompletedTaskId == completedTaskId && t.RecordStatus == RecordStatus.Active);
    }

    public async System.Threading.Tasks.Task<CompletedTask?> GetCompletedTaskByOriginalIdAsync(int originalTaskId)
    {
        return await _context.CompletedTasks
            .Include(t => t.Creator)
            .Include(t => t.Shop)
            .Include(t => t.Category)
            .Include(t => t.Priority)
            .Include(t => t.Attachments)
            .Include(t => t.TaskComments)
            .FirstOrDefaultAsync(t => t.OriginalTaskId == originalTaskId && t.RecordStatus == RecordStatus.Active);
    }

    public async System.Threading.Tasks.Task<(IEnumerable<CompletedTask> tasks, int totalCount)> GetCompletedTasksWithFiltersAsync(
        int? categoryId = null,
        string? searchQuery = null,
        int? priorityId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageNumber = 1,
        int pageSize = 50)
    {
        var query = _context.CompletedTasks
            .Include(t => t.Creator)
            .Include(t => t.Shop)
            .Include(t => t.Category)
            .Include(t => t.Priority)
            .Include(t => t.Attachments)
            .Where(t => t.RecordStatus == RecordStatus.Active)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId.Value);

        if (!string.IsNullOrEmpty(searchQuery))
            query = query.Where(t => t.Description.Contains(searchQuery) ||
                                   (t.SerialNumber != null && t.SerialNumber.Contains(searchQuery)) ||
                                   (t.PartNumber != null && t.PartNumber.Contains(searchQuery)));

        if (priorityId.HasValue)
            query = query.Where(t => t.PriorityId == priorityId.Value);

        if (fromDate.HasValue)
            query = query.Where(t => t.CompletedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(t => t.CompletedAt <= toDate.Value);

        var totalCount = await query.CountAsync();

        var tasks = await query
            .OrderByDescending(t => t.CompletedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (tasks, totalCount);
    }

    public async System.Threading.Tasks.Task<CompletedTask> MoveTaskToCompletedAsync(TaskEntity task)
    {
        // Create completed task from original task
        var completedTask = new CompletedTask
        {
            OriginalTaskId = task.TaskId,
            SerialNumber = task.SerialNumber,
            PartNumber = task.PartNumber,
            PoNumber = task.PoNumber,
            Description = task.Description,
            CategoryId = task.CategoryId,
            SubTypeId = task.SubTypeId,
            RequestTypeId = task.RequestTypeId,
            Status = task.Status,
            Comments = task.Comments,
            AssignedEngineer = task.AssignedEngineer,
            PriorityId = task.PriorityId,
            EstimatedCompletionDate = task.EstimatedCompletionDate,
            TargetCompletionDate = task.TargetCompletionDate,
            ActualCompletionDate = task.ActualCompletionDate ?? DateTime.UtcNow,
            AttachmentPath = task.AttachmentPath,
            AmendmentRequest = task.AmendmentRequest,
            AmendmentStatus = task.AmendmentStatus,
            AmendmentReviewedByTlId = task.AmendmentReviewedByTlId,
            IsDuplicate = task.IsDuplicate,
            DuplicateJustification = task.DuplicateJustification,
            RevisionNotes = task.RevisionNotes,
            ShowRevisionAlert = task.ShowRevisionAlert,
            ShopId = task.ShopId,
            CreatedBy = task.CreatedBy,
            IsMandatory = task.IsMandatory,
            CancelledBy = task.CancelledBy,
            CancellationReason = task.CancellationReason,
            CancelledAt = task.CancelledAt,
            TaskCreatedAt = task.CreatedAt,
            TaskUpdatedAt = task.UpdatedAt,
            CompletedAt = DateTime.UtcNow,
            MovedToCompletedAt = DateTime.UtcNow,
            RegisteredBy = "System",
            RegisteredDate = DateTime.UtcNow,
            LastUpdateDate = DateTime.UtcNow,
            UpdatedBy = "System",
            StartDate = DateTime.UtcNow,
            EndDate = new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc),
            TimeZoneInfo = "UTC",
            RecordStatus = RecordStatus.Active,
            IsReadOnly = false
        };

        // Copy task comments
        var taskComments = await _context.TaskComments
            .Where(c => c.TaskId == task.TaskId)
            .ToListAsync();

        foreach (var comment in taskComments)
        {
            var completedComment = new CompletedTaskComment
            {
                AuthorId = comment.AuthorId,
                CommentText = comment.Content, // TaskComment uses Content property
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                RegisteredBy = "System",
                RegisteredDate = DateTime.UtcNow,
                LastUpdateDate = DateTime.UtcNow,
                UpdatedBy = "System",
                StartDate = DateTime.UtcNow,
                EndDate = new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                TimeZoneInfo = "UTC",
                RecordStatus = RecordStatus.Active,
                IsReadOnly = false
            };
            completedTask.TaskComments.Add(completedComment);
        }

        // Copy task attachments
        var taskAttachments = await _context.TaskAttachments
            .Where(a => a.TaskId == task.TaskId)
            .ToListAsync();

        foreach (var attachment in taskAttachments)
        {
            var completedAttachment = new CompletedTaskAttachment
            {
                FileName = attachment.FileName,
                FilePath = attachment.FilePath,
                FileType = attachment.FileType,
                FileSize = attachment.FileSize,
                UploadedAt = attachment.UploadedAt,
                RegisteredBy = "System",
                RegisteredDate = DateTime.UtcNow,
                LastUpdateDate = DateTime.UtcNow,
                UpdatedBy = "System",
                StartDate = DateTime.UtcNow,
                EndDate = new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                TimeZoneInfo = "UTC",
                RecordStatus = RecordStatus.Active,
                IsReadOnly = false
            };
            completedTask.Attachments.Add(completedAttachment);
        }

        // Add to completed tasks
        _context.CompletedTasks.Add(completedTask);

        // Remove from active tasks (soft delete)
        task.RecordStatus = RecordStatus.Deleted;
        task.UpdatedAt = DateTime.UtcNow;
        _context.Tasks.Update(task);

        // Delete task comments and attachments
        _context.TaskComments.RemoveRange(taskComments);
        _context.TaskAttachments.RemoveRange(taskAttachments);

        await _context.SaveChangesAsync();

        return completedTask;
    }

    public async System.Threading.Tasks.Task<TaskEntity?> ReopenCompletedTaskAsync(int completedTaskId)
    {
        var completedTask = await _context.CompletedTasks
            .Include(t => t.TaskComments)
            .Include(t => t.Attachments)
            .FirstOrDefaultAsync(t => t.CompletedTaskId == completedTaskId);

        if (completedTask == null)
            return null;

        // Create new active task from completed task
        var task = new TaskEntity
        {
            SerialNumber = completedTask.SerialNumber,
            PartNumber = completedTask.PartNumber,
            PoNumber = completedTask.PoNumber,
            Description = completedTask.Description,
            CategoryId = completedTask.CategoryId,
            SubTypeId = completedTask.SubTypeId,
            RequestTypeId = completedTask.RequestTypeId,
            Status = TaskStatusEnum.Pending, // Reset to pending when reopened
            Comments = completedTask.Comments,
            AssignedEngineer = completedTask.AssignedEngineer,
            PriorityId = completedTask.PriorityId,
            EstimatedCompletionDate = completedTask.EstimatedCompletionDate,
            TargetCompletionDate = completedTask.TargetCompletionDate,
            ActualCompletionDate = null, // Clear completion date
            AttachmentPath = completedTask.AttachmentPath,
            AmendmentRequest = completedTask.AmendmentRequest,
            AmendmentStatus = completedTask.AmendmentStatus,
            AmendmentReviewedByTlId = completedTask.AmendmentReviewedByTlId,
            IsDuplicate = completedTask.IsDuplicate,
            DuplicateJustification = completedTask.DuplicateJustification,
            RevisionNotes = completedTask.RevisionNotes,
            ShowRevisionAlert = true, // Show alert for reopened tasks
            ShopId = completedTask.ShopId,
            CreatedBy = completedTask.CreatedBy,
            IsMandatory = completedTask.IsMandatory,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RegisteredBy = "System",
            RegisteredDate = DateTime.UtcNow,
            LastUpdateDate = DateTime.UtcNow,
            UpdatedBy = "System",
            StartDate = DateTime.UtcNow,
            EndDate = new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc),
            TimeZoneInfo = "UTC",
            RecordStatus = RecordStatus.Active,
            IsReadOnly = false
        };

        // Copy comments back
        foreach (var comment in completedTask.TaskComments)
        {
            var taskComment = new TaskComment
            {
                AuthorId = comment.AuthorId,
                Content = comment.CommentText, // CompletedTaskComment uses CommentText property
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                RegisteredBy = "System",
                RegisteredDate = DateTime.UtcNow,
                LastUpdateDate = DateTime.UtcNow,
                UpdatedBy = "System",
                StartDate = DateTime.UtcNow,
                EndDate = new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                TimeZoneInfo = "UTC",
                RecordStatus = RecordStatus.Active,
                IsReadOnly = false
            };
            task.TaskComments.Add(taskComment);
        }

        // Copy attachments back
        foreach (var attachment in completedTask.Attachments)
        {
            var taskAttachment = new TaskAttachment
            {
                FileName = attachment.FileName,
                FilePath = attachment.FilePath,
                FileType = attachment.FileType,
                FileSize = attachment.FileSize,
                UploadedAt = attachment.UploadedAt,
                RegisteredBy = "System",
                RegisteredDate = DateTime.UtcNow,
                LastUpdateDate = DateTime.UtcNow,
                UpdatedBy = "System",
                StartDate = DateTime.UtcNow,
                EndDate = new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                TimeZoneInfo = "UTC",
                RecordStatus = RecordStatus.Active,
                IsReadOnly = false
            };
            task.Attachments.Add(taskAttachment);
        }

        // Add reopened task
        _context.Tasks.Add(task);

        // Remove from completed tasks (soft delete)
        completedTask.RecordStatus = RecordStatus.Deleted;
        _context.CompletedTasks.Update(completedTask);

        await _context.SaveChangesAsync();

        return task;
    }

    public async System.Threading.Tasks.Task<bool> DeleteCompletedTaskAsync(int completedTaskId)
    {
        var completedTask = await _context.CompletedTasks
            .FirstOrDefaultAsync(t => t.CompletedTaskId == completedTaskId && t.RecordStatus == RecordStatus.Active);

        if (completedTask == null)
            return false;

        // Soft delete the completed task
        completedTask.RecordStatus = RecordStatus.Deleted;
        completedTask.LastUpdateDate = DateTime.UtcNow;
        completedTask.UpdatedBy = "System";

        _context.CompletedTasks.Update(completedTask);
        await _context.SaveChangesAsync();

        return true;
    }
}