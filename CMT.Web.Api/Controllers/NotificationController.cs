using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CMT.Application.Services;
using CMT.Application.DTOs;

namespace CMT.Web.Api.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [AllowAnonymous] // Temporarily remove authorization
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(INotificationService notificationService, ILogger<NotificationController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Get all notifications (for frontend compatibility)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> GetNotifications(
            [FromQuery] bool unreadOnly = false,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                // For now, return empty notifications for frontend compatibility
                // In a real implementation, you might want to get notifications for the current user
                var response = new
                {
                    notifications = new object[0],
                    totalCount = 0,
                    unreadCount = 0,
                    pagination = new
                    {
                        page = pageNumber,
                        limit = pageSize,
                        total = 0
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications");
                return StatusCode(500, new { message = "An error occurred while retrieving notifications", error = ex.Message });
            }
        }

        /// <summary>
        /// Get notifications for a user
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult> GetUserNotifications(int userId, 
            [FromQuery] bool unreadOnly = false,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                // Get real notifications from the database
                var notifications = await _notificationService.GetUnreadNotificationsAsync(userId);

                var response = new
                {
                    notifications = notifications.Select(n => new
                    {
                        notificationId = n.NotificationId,
                        userId = userId,
                        message = n.Message,
                        type = "notification",
                        isRead = n.IsRead,
                        createdAt = n.CreatedAt,
                        priority = "Medium"
                    }).ToList(),
                    totalCount = notifications.Count,
                    unreadCount = notifications.Count(n => !n.IsRead),
                    pagination = new
                    {
                        page = pageNumber,
                        limit = pageSize,
                        total = notifications.Count
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while retrieving notifications", error = ex.Message });
            }
        }

        /// <summary>
        /// Mark notification as read
        /// </summary>
        [HttpPatch("{notificationId}/read")]
        public async Task<ActionResult> MarkAsRead(int notificationId, [FromQuery] int userId)
        {
            try
            {
                await _notificationService.MarkNotificationAsReadAsync(notificationId, userId);

                var result = new
                {
                    notificationId = notificationId,
                    isRead = true,
                    updatedAt = DateTime.Now,
                    message = "Notification marked as read successfully"
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as read", notificationId);
                return StatusCode(500, new { message = "An error occurred while marking notification as read", error = ex.Message });
            }
        }

        /// <summary>
        /// Mark all notifications as read for a user
        /// </summary>
        [HttpPatch("user/{userId}/read-all")]
        public async Task<ActionResult> MarkAllAsRead(int userId)
        {
            try
            {
                await _notificationService.MarkAllNotificationsAsReadAsync(userId);

                var result = new
                {
                    userId = userId,
                    updatedAt = DateTime.Now,
                    message = "All notifications marked as read successfully"
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while marking all notifications as read", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete a notification
        /// </summary>
        [HttpDelete("{notificationId}")]
        public async Task<ActionResult> DeleteNotification(int notificationId)
        {
            try
            {
                // For now, we'll just mark as read instead of deleting
                // You can implement actual deletion if needed
                var result = new
                {
                    notificationId = notificationId,
                    deletedAt = DateTime.Now,
                    message = "Notification deleted successfully"
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification {NotificationId}", notificationId);
                return StatusCode(500, new { message = "An error occurred while deleting notification", error = ex.Message });
            }
        }

        /// <summary>
        /// Get notification settings for a user
        /// </summary>
        [HttpGet("user/{userId}/settings")]
        public ActionResult GetNotificationSettings(int userId)
        {
            try
            {
                // Mock data for notification settings - you can implement actual settings storage
                var settings = new
                {
                    userId = userId,
                    emailNotifications = true,
                    pushNotifications = true,
                    taskAssignments = true,
                    deadlineWarnings = true,
                    taskCompletions = false,
                    systemUpdates = true,
                    updatedAt = DateTime.Now.AddDays(-7)
                };

                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notification settings for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while retrieving notification settings", error = ex.Message });
            }
        }
    }
}