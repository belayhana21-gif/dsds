using CMT.Application.Services;
using CMT.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace CMT.Web.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompletedTaskController : ControllerBase
{
    private readonly ICompletedTaskService _completedTaskService;
    private readonly ILogger<CompletedTaskController> _logger;

    public CompletedTaskController(ICompletedTaskService completedTaskService, ILogger<CompletedTaskController> logger)
    {
        _completedTaskService = completedTaskService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCompletedTasks()
    {
        try
        {
            var tasks = await _completedTaskService.GetAllCompletedTasksAsync();
            return Ok(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving completed tasks");
            return StatusCode(500, new { message = "An error occurred while retrieving completed tasks" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCompletedTaskById(int id)
    {
        try
        {
            var task = await _completedTaskService.GetCompletedTaskByIdAsync(id);
            if (task == null)
                return NotFound(new { message = $"Completed task with ID {id} not found" });

            return Ok(task);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving completed task {TaskId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the completed task" });
        }
    }

    [HttpGet("filter")]
    public async Task<IActionResult> GetCompletedTasksWithFilters(
        [FromQuery] int? categoryId = null,
        [FromQuery] string? searchQuery = null,
        [FromQuery] int? priorityId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var (tasks, totalCount) = await _completedTaskService.GetCompletedTasksWithFiltersAsync(
                categoryId, searchQuery, priorityId, fromDate, toDate, pageNumber, pageSize);

            return Ok(new
            {
                tasks,
                totalCount,
                pageNumber,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error filtering completed tasks");
            return StatusCode(500, new { message = "An error occurred while filtering completed tasks" });
        }
    }

    [HttpPost("{id}/reopen")]
    public async Task<IActionResult> ReopenCompletedTask(int id)
    {
        try
        {
            var task = await _completedTaskService.ReopenCompletedTaskAsync(id);
            if (task == null)
                return NotFound(new { message = $"Completed task with ID {id} not found" });

            return Ok(new { message = "Task reopened successfully", task });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reopening completed task {TaskId}", id);
            return StatusCode(500, new { message = "An error occurred while reopening the task" });
        }
    }
}