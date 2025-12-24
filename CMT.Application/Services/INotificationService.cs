using Microsoft.EntityFrameworkCore;
using CMT.Application.DTOs;
using CMT.Domain.Data;

namespace CMT.Application.Services;

public interface INotificationService
{
    System.Threading.Tasks.Task NotifyAssignedEngineersAsync(int taskId, IEnumerable<string> engineerNames, CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task NotifyTaskStatusChangeAsync(int taskId, string newStatus, string changedBy, CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task NotifyAmendmentRequestAsync(int taskId, string requesterName, string reason, CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task<List<NotificationDto>> GetUnreadNotificationsAsync(int userId, CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task MarkNotificationAsReadAsync(int notificationId, int userId, CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task MarkAllNotificationsAsReadAsync(int userId, CancellationToken cancellationToken = default);
}

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationHubService _hubService;

    public NotificationService(ApplicationDbContext context, INotificationHubService hubService)
    {
        _context = context;
        _hubService = hubService;
    }

    public async System.Threading.Tasks.Task NotifyAssignedEngineersAsync(int taskId, IEnumerable<string> engineerNames, CancellationToken cancellationToken = default)
    {
        foreach (var engineerName in engineerNames.Where(name => !string.IsNullOrEmpty(name) && name != "Unassigned"))
        {
            var engineer = await _context.Users
                .FirstOrDefaultAsync(u => u.FullName == engineerName.Trim(), cancellationToken);

            if (engineer != null)
            {
                var notification = new CMT.Domain.Models.Notification
                {
                    RecipientId = engineer.UserId,
                    Message = $"New task assigned to you: Task TC-{taskId}",
                    IsRead = false
                };

                _context.Notifications.Add(notification);
                
                // Send real-time notification
                await _hubService.SendNotificationToUserAsync(engineer.UserId.ToString(), notification.Message);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async System.Threading.Tasks.Task NotifyTaskStatusChangeAsync(int taskId, string newStatus, string changedBy, CancellationToken cancellationToken = default)
    {
        var task = await _context.Tasks
            .Include(t => t.Creator)
            .FirstOrDefaultAsync(t => t.TaskId == taskId, cancellationToken);

        if (task == null) return;

        var message = $"Task TC-{taskId} status changed to {newStatus} by {changedBy}";

        // Notify task creator
        if (task.Creator != null)
        {
            var notification = new CMT.Domain.Models.Notification
            {
                RecipientId = task.Creator.UserId,
                Message = message,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _hubService.SendNotificationToUserAsync(task.Creator.UserId.ToString(), message);
        }

        // Notify assigned engineers
        if (!string.IsNullOrEmpty(task.AssignedEngineer) && task.AssignedEngineer != "Unassigned")
        {
            var assignedEngineers = task.AssignedEngineer.Split(',').Select(e => e.Trim());
            foreach (var engineerName in assignedEngineers)
            {
                var engineer = await _context.Users
                    .FirstOrDefaultAsync(u => u.FullName == engineerName, cancellationToken);

                if (engineer != null)
                {
                    var notification = new CMT.Domain.Models.Notification
                    {
                        RecipientId = engineer.UserId,
                        Message = message,
                        IsRead = false
                    };

                    _context.Notifications.Add(notification);
                    await _hubService.SendNotificationToUserAsync(engineer.UserId.ToString(), message);
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async System.Threading.Tasks.Task NotifyAmendmentRequestAsync(int taskId, string requesterName, string reason, CancellationToken cancellationToken = default)
    {
        // Get all team leaders for notification
        var teamLeaders = await _context.Users
            .Where(u => u.Role == CMT.Domain.Models.UserRole.TeamLeader && u.Status == CMT.Domain.Models.UserStatus.Active)
            .ToListAsync(cancellationToken);

        var message = $"Amendment request for Task TC-{taskId} by {requesterName}. Reason: {reason.Substring(0, Math.Min(100, reason.Length))}...";

        foreach (var teamLeader in teamLeaders)
        {
            var notification = new CMT.Domain.Models.Notification
            {
                RecipientId = teamLeader.UserId,
                Message = message,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _hubService.SendNotificationToUserAsync(teamLeader.UserId.ToString(), message);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async System.Threading.Tasks.Task<List<NotificationDto>> GetUnreadNotificationsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var notifications = await _context.Notifications
            .Where(n => n.RecipientId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto
            {
                NotificationId = n.NotificationId,
                Message = n.Message,
                CreatedAt = n.CreatedAt,
                IsRead = n.IsRead
            })
            .ToListAsync(cancellationToken);

        return notifications ?? new List<NotificationDto>();
    }

    public async System.Threading.Tasks.Task MarkNotificationAsReadAsync(int notificationId, int userId, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.RecipientId == userId, cancellationToken);

        if (notification != null)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async System.Threading.Tasks.Task MarkAllNotificationsAsReadAsync(int userId, CancellationToken cancellationToken = default)
    {
        var notifications = await _context.Notifications
            .Where(n => n.RecipientId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}