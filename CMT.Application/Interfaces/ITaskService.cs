using CMT.Domain.Models;
using TaskEntity = CMT.Domain.Models.Task;

namespace CMT.Application.Services
{
    public interface ITaskService
    {
        Task<IEnumerable<TaskEntity>> GetAllTasksAsync();
        Task<TaskEntity?> GetTaskByIdAsync(int taskId);
        Task<(IEnumerable<TaskEntity> tasks, int totalCount)> GetTasksWithFiltersAsync(
            int? categoryId = null,
            string? statusFilter = null,
            string? searchQuery = null,
            int? priorityId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int pageNumber = 1,
            int pageSize = 50);
        Task<TaskEntity> CreateTaskAsync(TaskEntity task);
        Task<TaskEntity> UpdateTaskAsync(TaskEntity task, int updatedByUserId, string updatedByUserName);
        Task<bool> DeleteTaskAsync(int taskId);
        Task<IEnumerable<TaskEntity>> GetTasksByUserIdAsync(int userId);
        Task<PerformanceMetrics> GetUserPerformanceMetricsAsync(int userId);
        Task<DashboardMetrics> GetDashboardMetricsAsync();
    }

    public class PerformanceMetrics
    {
        public double CompletionRate { get; set; }
        public double AvgDelay { get; set; }
        public int TasksCompleted { get; set; }
        public int PendingTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int TotalTasks { get; set; }
    }

    public class DashboardMetrics
    {
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int PendingTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int OverdueTasks { get; set; }
        public double CompletionRate { get; set; }
        public string AverageCompletionTime { get; set; } = "0 days";
        public IEnumerable<TaskEntity> RecentTasks { get; set; } = new List<TaskEntity>();
    }
}