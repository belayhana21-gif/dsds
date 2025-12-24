using SystemTask = System.Threading.Tasks.Task;

namespace CMT.Application.Services;

public interface INotificationHubService
{
    SystemTask SendNotificationToUserAsync(string userId, string message);
    SystemTask SendNotificationToRoleAsync(string role, string message);
    SystemTask SendBroadcastNotificationAsync(string message);
    SystemTask SendTaskAssignedNotificationAsync(string userId, string taskId, string serialNumber);
    SystemTask SendTaskCompletedNotificationAsync(string userId, string taskId, string serialNumber);
    SystemTask SendTaskStatusChangedNotificationAsync(string userId, string taskId, string serialNumber, string status);
}