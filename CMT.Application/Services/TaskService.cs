using CMT.Application.Services;
using CMT.Domain.Data;
using CMT.Domain.Models;
using Microsoft.EntityFrameworkCore;
using TaskEntity = CMT.Domain.Models.Task;
using DomainTaskStatus = CMT.Domain.Models.TaskStatus;

namespace CMT.Application.Services
{
    public class TaskService : ITaskService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public TaskService(ApplicationDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<IEnumerable<TaskEntity>> GetAllTasksAsync()
        {
            return await _context.Tasks
                .Where(t => t.RecordStatus == RecordStatus.Active) // Exclude soft-deleted tasks
                .Include(t => t.Creator)
                .Include(t => t.Shop)
                .Include(t => t.Category)
                .Include(t => t.Priority)
                .Include(t => t.Attachments)
                .ToListAsync();
        }

        public async Task<TaskEntity?> GetTaskByIdAsync(int taskId)
        {
            return await _context.Tasks
                .Where(t => t.RecordStatus == RecordStatus.Active) // Exclude soft-deleted tasks
                .Include(t => t.Creator)
                .Include(t => t.Shop)
                .Include(t => t.Category)
                .Include(t => t.Priority)
                .Include(t => t.Attachments)
                .FirstOrDefaultAsync(t => t.TaskId == taskId);
        }

        public async Task<(IEnumerable<TaskEntity> tasks, int totalCount)> GetTasksWithFiltersAsync(
            int? categoryId = null,
            string? statusFilter = null,
            string? searchQuery = null,
            int? priorityId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int pageNumber = 1,
            int pageSize = 50)
        {
            var query = _context.Tasks
                .Where(t => t.RecordStatus == RecordStatus.Active) // Exclude soft-deleted tasks
                .Include(t => t.Creator)
                .Include(t => t.Shop)
                .Include(t => t.Category)
                .Include(t => t.Priority)
                .Include(t => t.Attachments)
                .AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(t => t.CategoryId == categoryId.Value);

            if (!string.IsNullOrEmpty(statusFilter) && Enum.TryParse<DomainTaskStatus>(statusFilter, out var status))
                query = query.Where(t => t.Status == status);

            if (!string.IsNullOrEmpty(searchQuery))
                query = query.Where(t => t.Description.Contains(searchQuery) || 
                                       (t.SerialNumber != null && t.SerialNumber.Contains(searchQuery)) ||
                                       (t.PartNumber != null && t.PartNumber.Contains(searchQuery)));

            if (priorityId.HasValue)
                query = query.Where(t => t.PriorityId == priorityId.Value);

            if (fromDate.HasValue)
                query = query.Where(t => t.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(t => t.CreatedAt <= toDate.Value);

            var totalCount = await query.CountAsync();

            var tasks = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (tasks, totalCount);
        }

        public async Task<TaskEntity> CreateTaskAsync(TaskEntity task)
        {
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            // Send notification for task assignment
            if (!string.IsNullOrEmpty(task.AssignedEngineer) && task.AssignedEngineer != "Unassigned")
            {
                var engineerNames = task.AssignedEngineer.Split(',').Select(e => e.Trim());
                await _notificationService.NotifyAssignedEngineersAsync(task.TaskId, engineerNames);
            }

            return task;
        }

        public async Task<TaskEntity> UpdateTaskAsync(TaskEntity task, int updatedByUserId, string updatedByUserName)
        {
            var existingTask = await _context.Tasks
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TaskId == task.TaskId);

            if (existingTask != null)
            {
                // Check if status changed
                if (existingTask.Status != task.Status)
                {
                    var changedBy = updatedByUserName ?? "System";
                    await _notificationService.NotifyTaskStatusChangeAsync(task.TaskId, task.Status.ToString(), changedBy);
                }

                // Check if assignment changed
                if (existingTask.AssignedEngineer != task.AssignedEngineer && 
                    !string.IsNullOrEmpty(task.AssignedEngineer) && 
                    task.AssignedEngineer != "Unassigned")
                {
                    var engineerNames = task.AssignedEngineer.Split(',').Select(e => e.Trim());
                    await _notificationService.NotifyAssignedEngineersAsync(task.TaskId, engineerNames);
                }
            }

            _context.Tasks.Update(task);
            await _context.SaveChangesAsync();
            return task;
        }

        public async Task<bool> DeleteTaskAsync(int taskId)
        {
            var task = await _context.Tasks.FindAsync(taskId);
            if (task == null) return false;

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<TaskEntity>> GetTasksByUserIdAsync(int userId)
        {
            return await _context.Tasks
                .Where(t => t.RecordStatus == RecordStatus.Active) // Exclude soft-deleted tasks
                .Include(t => t.Creator)
                .Include(t => t.Shop)
                .Include(t => t.Category)
                .Include(t => t.Priority)
                .Include(t => t.Attachments)
                .Where(t => t.CreatedBy == userId)
                .ToListAsync();
        }

        public async Task<PerformanceMetrics> GetUserPerformanceMetricsAsync(int userId)
        {
            var userTasks = await GetTasksByUserIdAsync(userId);
            var totalTasks = userTasks.Count();
            var completedTasks = userTasks.Count(t => t.Status == DomainTaskStatus.Completed);
            var pendingTasks = userTasks.Count(t => t.Status == DomainTaskStatus.Pending);
            var overdueTasks = userTasks.Count(t => t.EstimatedCompletionDate < DateTime.Now && t.Status != DomainTaskStatus.Completed);

            return new PerformanceMetrics
            {
                TotalTasks = totalTasks,
                TasksCompleted = completedTasks,
                PendingTasks = pendingTasks,
                OverdueTasks = overdueTasks,
                CompletionRate = totalTasks > 0 ? (double)completedTasks / totalTasks : 0,
                AvgDelay = 0 // Calculate based on your business logic
            };
        }

        public async Task<DashboardMetrics> GetDashboardMetricsAsync()
        {
            var allTasks = await GetAllTasksAsync();
            var totalTasks = allTasks.Count();
            var completedTasks = allTasks.Count(t => t.Status == DomainTaskStatus.Completed);
            var pendingTasks = allTasks.Count(t => t.Status == DomainTaskStatus.Pending);
            var inProgressTasks = allTasks.Count(t => t.Status == DomainTaskStatus.InProgress);
            var overdueTasks = allTasks.Count(t => t.EstimatedCompletionDate < DateTime.Now && t.Status != DomainTaskStatus.Completed);

            return new DashboardMetrics
            {
                TotalTasks = totalTasks,
                CompletedTasks = completedTasks,
                PendingTasks = pendingTasks,
                InProgressTasks = inProgressTasks,
                OverdueTasks = overdueTasks,
                CompletionRate = totalTasks > 0 ? (double)completedTasks / totalTasks : 0,
                AverageCompletionTime = "0 days",
                RecentTasks = allTasks.OrderByDescending(t => t.CreatedAt).Take(5)
            };
        }
    }
}