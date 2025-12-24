using Microsoft.EntityFrameworkCore;
using CMT.Application.DTOs;
using CMT.Domain.Data;
using CMT.Domain.Models;
using CMT.Application.Interfaces;
using TaskEntity = CMT.Domain.Models.Task;

namespace CMT.Application.Services;

public class AmendmentService : IAmendmentService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public AmendmentService(ApplicationDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<ServiceResult<AmendmentDto>> RequestAmendmentAsync(int taskId, int requesterId, string reason, CancellationToken cancellationToken = default)
    {
        var task = await _context.Tasks
            .Include(t => t.Creator)
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.TaskId == taskId, cancellationToken);

        if (task == null)
        {
            return ServiceResult<AmendmentDto>.Failure("Task not found");
        }

        if (task.AmendmentRequest)
        {
            return ServiceResult<AmendmentDto>.Failure("Amendment request already exists for this task");
        }

        var requester = await _context.Users.FindAsync(requesterId);
        if (requester == null)
        {
            return ServiceResult<AmendmentDto>.Failure("Requester not found");
        }

        // Update task with amendment request
        task.AmendmentRequest = true;
        task.AmendmentStatus = AmendmentStatus.PendingTLReview;
        task.RevisionNotes = reason;
        task.ShowRevisionAlert = true;
        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Send notifications to Team Leaders
        await _notificationService.NotifyAmendmentRequestAsync(taskId, requester.FullName, reason, cancellationToken);

        var amendmentDto = new AmendmentDto
        {
            TaskId = task.TaskId,
            TaskDescription = task.Description,
            SerialNumber = task.SerialNumber ?? "",
            PartNumber = task.PartNumber ?? "",
            Status = task.AmendmentStatus ?? AmendmentStatus.PendingTLReview,
            RequestReason = reason,
            RequestedAt = task.UpdatedAt,
            RequesterName = requester.FullName,
            RequesterId = requesterId,
            RequiresDirectorApproval = DetermineIfDirectorApprovalRequired(task)
        };

        return ServiceResult<AmendmentDto>.Success(amendmentDto);
    }

    public async Task<ServiceResult<AmendmentDto>> ReviewAmendmentAsync(int taskId, int reviewerId, AmendmentStatus decision, string reviewNotes, CancellationToken cancellationToken = default)
    {
        var task = await _context.Tasks
            .Include(t => t.Creator)
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.TaskId == taskId, cancellationToken);

        if (task == null)
        {
            return ServiceResult<AmendmentDto>.Failure("Task not found");
        }

        if (!task.AmendmentRequest || task.AmendmentStatus != AmendmentStatus.PendingTLReview)
        {
            return ServiceResult<AmendmentDto>.Failure("No pending amendment request found for this task");
        }

        var reviewer = await _context.Users.FindAsync(reviewerId);
        if (reviewer == null || reviewer.Role != CMT.Domain.Models.UserRole.TeamLeader)
        {
            return ServiceResult<AmendmentDto>.Failure("Only Team Leaders can review amendment requests");
        }

        // Update task with review decision
        task.AmendmentStatus = decision;
        task.AmendmentReviewedByTlId = reviewerId;
        task.RevisionNotes = $"{task.RevisionNotes}\\n\\nTL Review: {reviewNotes}";
        task.UpdatedAt = DateTime.UtcNow;

        if (decision == AmendmentStatus.Approved)
        {
            task.ShowRevisionAlert = false;
        }
        else if (decision == AmendmentStatus.Rejected)
        {
            task.AmendmentRequest = false;
            task.ShowRevisionAlert = false;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Send notification about review decision
        var statusMessage = decision == AmendmentStatus.Approved ? "approved" : 
                           decision == AmendmentStatus.Rejected ? "rejected" : "forwarded to Director";
        
        await _notificationService.NotifyTaskStatusChangeAsync(taskId, $"Amendment {statusMessage}", reviewer.FullName, cancellationToken);

        var amendmentDto = new AmendmentDto
        {
            TaskId = task.TaskId,
            TaskDescription = task.Description,
            SerialNumber = task.SerialNumber ?? "",
            PartNumber = task.PartNumber ?? "",
            Status = task.AmendmentStatus ?? AmendmentStatus.PendingTLReview,
            RequestReason = task.RevisionNotes ?? "",
            ReviewNotes = reviewNotes,
            RequestedAt = task.CreatedAt,
            ReviewedAt = task.UpdatedAt,
            RequesterName = task.Creator?.FullName ?? "",
            ReviewerName = reviewer.FullName,
            RequesterId = task.CreatedBy,
            ReviewerId = reviewerId
        };

        return ServiceResult<AmendmentDto>.Success(amendmentDto);
    }

    public async Task<ServiceResult<AmendmentDto>> ApproveAmendmentAsync(int taskId, int approverId, string approvalNotes, CancellationToken cancellationToken = default)
    {
        return await ReviewAmendmentAsync(taskId, approverId, AmendmentStatus.Approved, approvalNotes, cancellationToken);
    }

    public async Task<ServiceResult<AmendmentDto>> RejectAmendmentAsync(int taskId, int rejectorId, string rejectionReason, CancellationToken cancellationToken = default)
    {
        return await ReviewAmendmentAsync(taskId, rejectorId, AmendmentStatus.Rejected, rejectionReason, cancellationToken);
    }

    public async Task<ServiceResult<AmendmentDto>> ForwardToDirectorAsync(int taskId, int teamLeaderId, string forwardingNotes, CancellationToken cancellationToken = default)
    {
        return await ReviewAmendmentAsync(taskId, teamLeaderId, AmendmentStatus.ForwardedToDirector, forwardingNotes, cancellationToken);
    }

    public async Task<ServiceResult<List<AmendmentDto>>> GetPendingAmendmentsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return ServiceResult<List<AmendmentDto>>.Failure("User not found");
        }

        IQueryable<TaskEntity> query = _context.Tasks
            .Include(t => t.Creator)
            .Include(t => t.AmendmentReviewer)
            .Where(t => t.AmendmentRequest);

        // Filter based on user role
        if (user.Role == CMT.Domain.Models.UserRole.TeamLeader)
        {
            query = query.Where(t => t.AmendmentStatus == AmendmentStatus.PendingTLReview);
        }
        else if (user.Role == CMT.Domain.Models.UserRole.Director)
        {
            query = query.Where(t => t.AmendmentStatus == AmendmentStatus.ForwardedToDirector);
        }
        else
        {
            // Engineers can only see their own amendment requests
            query = query.Where(t => t.CreatedBy == userId);
        }

        var tasks = await query.ToListAsync(cancellationToken);

        var amendments = tasks.Select(t => new AmendmentDto
        {
            TaskId = t.TaskId,
            TaskDescription = t.Description,
            SerialNumber = t.SerialNumber ?? "",
            PartNumber = t.PartNumber ?? "",
            Status = t.AmendmentStatus ?? AmendmentStatus.PendingTLReview,
            RequestReason = t.RevisionNotes ?? "",
            RequestedAt = t.CreatedAt,
            ReviewedAt = t.UpdatedAt,
            RequesterName = t.Creator?.FullName ?? "",
            ReviewerName = t.AmendmentReviewer?.FullName ?? "",
            RequesterId = t.CreatedBy,
            ReviewerId = t.AmendmentReviewedByTlId,
            RequiresDirectorApproval = DetermineIfDirectorApprovalRequired(t)
        }).ToList();

        return ServiceResult<List<AmendmentDto>>.Success(amendments);
    }

    public async Task<ServiceResult<List<AmendmentDto>>> GetAmendmentHistoryAsync(int taskId, CancellationToken cancellationToken = default)
    {
        var task = await _context.Tasks
            .Include(t => t.Creator)
            .Include(t => t.AmendmentReviewer)
            .FirstOrDefaultAsync(t => t.TaskId == taskId, cancellationToken);

        if (task == null)
        {
            return ServiceResult<List<AmendmentDto>>.Failure("Task not found");
        }

        var amendments = new List<AmendmentDto>();

        if (task.AmendmentRequest || task.AmendmentStatus.HasValue)
        {
            amendments.Add(new AmendmentDto
            {
                TaskId = task.TaskId,
                TaskDescription = task.Description,
                SerialNumber = task.SerialNumber ?? "",
                PartNumber = task.PartNumber ?? "",
                Status = task.AmendmentStatus ?? AmendmentStatus.PendingTLReview,
                RequestReason = task.RevisionNotes ?? "",
                RequestedAt = task.CreatedAt,
                ReviewedAt = task.UpdatedAt,
                RequesterName = task.Creator?.FullName ?? "",
                ReviewerName = task.AmendmentReviewer?.FullName ?? "",
                RequesterId = task.CreatedBy,
                ReviewerId = task.AmendmentReviewedByTlId
            });
        }

        return ServiceResult<List<AmendmentDto>>.Success(amendments);
    }

    private static bool DetermineIfDirectorApprovalRequired(TaskEntity task)
    {
        // Business logic to determine if Director approval is required
        // For example: High priority tasks, certain categories, etc.
        return task.PriorityId == 1 || // Critical priority
               task.CategoryId == 1;    // AOG & CSD Components
    }
}