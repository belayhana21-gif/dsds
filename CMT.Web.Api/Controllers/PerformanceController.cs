using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CMT.Application.Services;
using CMT.Application.DTOs;

namespace CMT.Web.Api.Controllers
{
    [ApiController]
    [Route("api/performance")]
    [AllowAnonymous] // Temporarily remove authorization
    public class PerformanceController : ControllerBase
    {
        private readonly ITaskService _taskService;

        public PerformanceController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        /// <summary>
        /// Get engineer workload metrics (for frontend compatibility)
        /// </summary>
        [HttpGet("engineer-workload")]
        public async Task<ActionResult> GetEngineerWorkload()
        {
            try
            {
                // Mock data for engineer workload - replace with actual implementation
                var engineerWorkload = new
                {
                    engineers = new[]
                    {
                        new { 
                            engineerName = "John Smith", 
                            totalTasks = 15, 
                            completedTasks = 12, 
                            pendingTasks = 3, 
                            workloadPercentage = 80 
                        },
                        new { 
                            engineerName = "Sarah Johnson", 
                            totalTasks = 18, 
                            completedTasks = 15, 
                            pendingTasks = 3, 
                            workloadPercentage = 83 
                        },
                        new { 
                            engineerName = "Mike Davis", 
                            totalTasks = 10, 
                            completedTasks = 8, 
                            pendingTasks = 2, 
                            workloadPercentage = 80 
                        },
                        new { 
                            engineerName = "Emily Wilson", 
                            totalTasks = 22, 
                            completedTasks = 18, 
                            pendingTasks = 4, 
                            workloadPercentage = 82 
                        }
                    },
                    summary = new
                    {
                        totalEngineers = 4,
                        averageWorkload = 81.25,
                        totalTasks = 65,
                        totalCompleted = 53,
                        totalPending = 12
                    }
                };

                return Ok(engineerWorkload);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving engineer workload", error = ex.Message });
            }
        }

        /// <summary>
        /// Get overall performance metrics
        /// </summary>
        [HttpGet("overview")]
        public async Task<ActionResult> GetOverviewMetrics()
        {
            try
            {
                var metrics = await _taskService.GetDashboardMetricsAsync();
                
                var overviewMetrics = new
                {
                    totalTasks = metrics.TotalTasks,
                    completedTasks = metrics.CompletedTasks,
                    pendingTasks = metrics.PendingTasks,
                    inProgressTasks = metrics.InProgressTasks,
                    overdueTasks = metrics.OverdueTasks,
                    completionRate = metrics.CompletionRate,
                    averageCompletionTime = metrics.AverageCompletionTime
                };

                return Ok(overviewMetrics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving overview metrics", error = ex.Message });
            }
        }

        /// <summary>
        /// Get performance metrics by shop
        /// </summary>
        [HttpGet("shop/{shopId}")]
        public ActionResult GetShopPerformance(int shopId)
        {
            try
            {
                // Mock data for shop performance - replace with actual implementation
                var shopPerformance = new
                {
                    shopId = shopId,
                    shopName = $"Shop {shopId}",
                    totalTasks = 25,
                    completedTasks = 20,
                    pendingTasks = 3,
                    inProgressTasks = 2,
                    completionRate = 0.80,
                    averageCompletionTime = "5.2 days",
                    efficiency = 85.5
                };

                return Ok(shopPerformance);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving shop performance", error = ex.Message });
            }
        }

        /// <summary>
        /// Get performance metrics by user
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult> GetUserPerformance(int userId)
        {
            try
            {
                var userMetrics = await _taskService.GetUserPerformanceMetricsAsync(userId);
                
                var userPerformance = new
                {
                    userId = userId,
                    totalTasks = userMetrics.TotalTasks,
                    completedTasks = userMetrics.TasksCompleted,
                    pendingTasks = userMetrics.PendingTasks,
                    overdueTasks = userMetrics.OverdueTasks,
                    completionRate = userMetrics.CompletionRate,
                    averageDelay = userMetrics.AvgDelay
                };

                return Ok(userPerformance);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving user performance", error = ex.Message });
            }
        }

        /// <summary>
        /// Get performance trends over time
        /// </summary>
        [HttpGet("trends")]
        public ActionResult GetPerformanceTrends(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? period = "monthly")
        {
            try
            {
                // Mock data for performance trends - replace with actual implementation
                var trends = new
                {
                    period = period ?? "monthly",
                    startDate = startDate ?? DateTime.Now.AddMonths(-6),
                    endDate = endDate ?? DateTime.Now,
                    data = new[]
                    {
                        new { date = DateTime.Now.AddMonths(-5), completionRate = 0.75, tasksCompleted = 45 },
                        new { date = DateTime.Now.AddMonths(-4), completionRate = 0.80, tasksCompleted = 52 },
                        new { date = DateTime.Now.AddMonths(-3), completionRate = 0.85, tasksCompleted = 48 },
                        new { date = DateTime.Now.AddMonths(-2), completionRate = 0.82, tasksCompleted = 55 },
                        new { date = DateTime.Now.AddMonths(-1), completionRate = 0.88, tasksCompleted = 62 },
                        new { date = DateTime.Now, completionRate = 0.90, tasksCompleted = 58 }
                    }
                };

                return Ok(trends);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving performance trends", error = ex.Message });
            }
        }
    }
}