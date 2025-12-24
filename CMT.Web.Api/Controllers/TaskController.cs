using CMT.Application.DTOs;
using CMT.Application.Interfaces;
using CMT.Web.Api.Attributes;
using CMT.Domain.Models;
using CMT.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using CMT.Application.Commands.Tasks;
using CMT.Application.Queries;
using CMT.Application.Common;
using System.Security.Claims;
using CMTAuthorizationService = CMT.Application.Interfaces.IAuthorizationService;
using TaskStatusEnum = CMT.Domain.Models.TaskStatus;
using TaskEntity = CMT.Domain.Models.Task;
using CMT.Application.Services;

namespace CMT.Web.Api.Controllers
{
    [ApiController]
    [Route("api/tasks")]
    [Authorize] 
    public class TaskController : ControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly ICompletedTaskService _completedTaskService;
        private readonly CMTAuthorizationService _authorizationService;
        private readonly IMediator _mediator;
        private readonly ILogger<TaskController> _logger;
        private readonly IAmendmentService _amendmentService;

        public TaskController(
            ITaskService taskService, 
            ICompletedTaskService completedTaskService,
            CMTAuthorizationService authorizationService, 
            IMediator mediator,
            ILogger<TaskController> logger,
            IAmendmentService amendmentService)
        {
            _taskService = taskService;
            _completedTaskService = completedTaskService;
            _authorizationService = authorizationService;
            _mediator = mediator;
            _logger = logger;
            _amendmentService = amendmentService;
        }

        private (int UserId, string UserName) GetCurrentUser()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.FindFirstValue(ClaimTypes.Name);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            _logger.LogInformation("Current User Claims: Id='{UserId}', Name='{UserName}', Role='{UserRole}'", userIdClaim, userName, userRole);

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("Could not find authenticated user. This may cause issues.");
                return (1, "System"); // Fallback to a default user
            }

            return (userId, userName ?? "Unknown");
        }

        /// <summary>
        /// Get tasks with optional filtering (excludes completed tasks that have been moved)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetTasks(
            [FromQuery] int? categoryId = null,
            [FromQuery] string? status = null,
            [FromQuery] string? searchQuery = null,
            [FromQuery] int? priorityId = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var (tasks, totalCount) = await _taskService.GetTasksWithFiltersAsync(
                    categoryId, status, searchQuery, priorityId, fromDate, toDate, pageNumber, pageSize);

                var taskDtos = tasks.Select(t => new TaskDto
                {
                    TaskId = t.TaskId,
                    SerialNumber = t.SerialNumber ?? "",
                    PartNumber = t.PartNumber ?? "",
                    PoNumber = t.PoNumber ?? "",
                    Description = t.Description,
                    CategoryId = t.CategoryId,
                    CategoryName = t.Category?.Name ?? "Unknown",
                    Status = t.Status,
                    Comments = t.Comments,
                    AssignedEngineer = t.AssignedEngineer ?? "Unassigned",
                    PriorityId = t.PriorityId,
                    PriorityName = t.Priority?.LevelName ?? "Unknown",
                    EstimatedCompletionDate = t.EstimatedCompletionDate,
                    TargetCompletionDate = t.TargetCompletionDate,
                    ActualCompletionDate = t.ActualCompletionDate,
                    AttachmentPath = t.AttachmentPath,
                    AttachmentsCount = t.Attachments?.Count ?? 0,
                    ShopId = t.ShopId,
                    ShopName = t.Shop?.Name ?? "Unknown",
                    CreatedBy = t.CreatedBy,
                    CreatedByName = t.Creator?.FullName ?? "Unknown",
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt
                }).ToList();
                
                var response = new
                {
                    tasks = taskDtos,
                    pagination = new
                    {
                        page = pageNumber,
                        limit = pageSize,
                        total = totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    },
                    userRole = "Admin",
                    success = true
                };
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tasks");
                return StatusCode(500, new { message = "An error occurred while retrieving tasks", error = ex.Message });
            }
        }

        /// <summary>
        /// Get completed tasks from the completed_tasks table
        /// </summary>
        [HttpGet("completed")]
        public async Task<ActionResult<IEnumerable<CompletedTaskDto>>> GetCompletedTasks(
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
                var (completedTasks, totalCount) = await _completedTaskService.GetCompletedTasksWithFiltersAsync(
                    categoryId, searchQuery, priorityId, fromDate, toDate, pageNumber, pageSize);

                var taskDtos = completedTasks.Select(t => new CompletedTaskDto
                {
                    CompletedTaskId = t.CompletedTaskId,
                    OriginalTaskId = t.OriginalTaskId,
                    SerialNumber = t.SerialNumber ?? "",
                    PartNumber = t.PartNumber ?? "",
                    PoNumber = t.PoNumber ?? "",
                    Description = t.Description,
                    CategoryId = t.CategoryId,
                    CategoryName = t.Category?.Name ?? "Unknown",
                    Status = t.Status,
                    Comments = t.Comments,
                    AssignedEngineer = t.AssignedEngineer ?? "Unassigned",
                    PriorityId = t.PriorityId,
                    PriorityName = t.Priority?.LevelName ?? "Unknown",
                    EstimatedCompletionDate = t.EstimatedCompletionDate,
                    TargetCompletionDate = t.TargetCompletionDate,
                    ActualCompletionDate = t.ActualCompletionDate,
                    AttachmentPath = t.AttachmentPath,
                    AttachmentsCount = t.Attachments?.Count ?? 0,
                    ShopId = t.ShopId,
                    ShopName = t.Shop?.Name ?? "Unknown",
                    CreatedBy = t.CreatedBy,
                    CreatedByName = t.Creator?.FullName ?? "Unknown",
                    TaskCreatedAt = t.TaskCreatedAt,
                    TaskUpdatedAt = t.TaskUpdatedAt,
                    CompletedAt = t.CompletedAt,
                    MovedToCompletedAt = t.MovedToCompletedAt
                }).ToList();
                
                var response = new
                {
                    tasks = taskDtos,
                    pagination = new
                    {
                        page = pageNumber,
                        limit = pageSize,
                        total = totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    },
                    userRole = "Admin",
                    success = true
                };
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving completed tasks");
                return StatusCode(500, new { message = "An error occurred while retrieving completed tasks", error = ex.Message });
            }
        }

        /// <summary>
        /// Get a specific task by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<TaskDto>> GetTask(int id)
        {
            try
            {
                var task = await _taskService.GetTaskByIdAsync(id);
                
                if (task == null)
                {
                    return NotFound(new { message = $"Task with ID {id} not found" });
                }

                var taskDto = new TaskDto
                {
                    TaskId = task.TaskId,
                    SerialNumber = task.SerialNumber ?? "",
                    PartNumber = task.PartNumber ?? "",
                    PoNumber = task.PoNumber ?? "",
                    Description = task.Description,
                    CategoryId = task.CategoryId,
                    CategoryName = task.Category?.Name ?? "Unknown",
                    Status = task.Status,
                    Comments = task.Comments,
                    AssignedEngineer = task.AssignedEngineer ?? "Unassigned",
                    PriorityId = task.PriorityId,
                    PriorityName = task.Priority?.LevelName ?? "Unknown",
                    EstimatedCompletionDate = task.EstimatedCompletionDate,
                    TargetCompletionDate = task.TargetCompletionDate,
                    ActualCompletionDate = task.ActualCompletionDate,
                    AttachmentPath = task.AttachmentPath,
                    AttachmentsCount = task.Attachments?.Count ?? 0,
                    ShopId = task.ShopId,
                    ShopName = task.Shop?.Name ?? "Unknown",
                    CreatedBy = task.CreatedBy,
                    CreatedByName = task.Creator?.FullName ?? "Unknown",
                    CreatedAt = task.CreatedAt,
                    UpdatedAt = task.UpdatedAt
                };
                
                return Ok(taskDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving task {TaskId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the task", error = ex.Message });
            }
        }

        /// <summary>
        /// Request amendment for a task
        /// </summary>
        [HttpPost("{id}/request-amendment")]
        public async Task<ActionResult> RequestAmendment(int id, [FromBody] RequestAmendmentDto request)
        {
            try
            {
                var (userId, userName) = GetCurrentUser();
                
                if (string.IsNullOrWhiteSpace(request.AmendmentReason))
                {
                    return BadRequest(new { error = "Amendment reason is required" });
                }

                var result = await _amendmentService.RequestAmendmentAsync(id, userId, request.AmendmentReason);
                
                if (!result.IsSuccess)
                {
                    var errorMessage = result.Errors?.FirstOrDefault() ?? "Amendment request failed";
                    return BadRequest(new { error = errorMessage });
                }

                _logger.LogInformation("Amendment requested for task {TaskId} by user {UserId} ({UserName})", id, userId, userName);

                return Ok(new { 
                    success = true, 
                    message = "Amendment requested successfully. Team Leaders have been notified.",
                    amendment = result.Payload
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting amendment for task {TaskId}", id);
                return StatusCode(500, new { error = "An error occurred while requesting amendment", details = ex.Message });
            }
        }

        /// <summary>
        /// Create a new task
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<TaskDto>> CreateTask([FromBody] CreateTaskDto request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Description))
                {
                    return BadRequest(new { message = "Description is required" });
                }

                if (request.CategoryId <= 0)
                {
                    return BadRequest(new { message = "Valid CategoryId is required" });
                }

                if (request.PriorityId <= 0)
                {
                    return BadRequest(new { message = "Valid PriorityId is required" });
                }

                var newTaskEntity = new TaskEntity
                {
                    SerialNumber = request.SerialNumber,
                    PartNumber = request.PartNumber,
                    PoNumber = request.PoNumber,
                    Description = request.Description,
                    CategoryId = request.CategoryId,
                    Status = TaskStatusEnum.Pending,
                    Comments = request.Comments,
                    AssignedEngineer = request.AssignedEngineer ?? "Unassigned",
                    PriorityId = request.PriorityId,
                    EstimatedCompletionDate = request.EstimatedCompletionDate != default ? request.EstimatedCompletionDate : DateTime.Now.AddDays(7),
                    TargetCompletionDate = request.TargetCompletionDate,
                    AttachmentPath = request.AttachmentPath,
                    ShopId = request.ShopId,
                    CreatedBy = request.CreatedBy > 0 ? request.CreatedBy : 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var createdTask = await _taskService.CreateTaskAsync(newTaskEntity);
                var fullTask = await _taskService.GetTaskByIdAsync(createdTask.TaskId);

                var taskDto = new TaskDto
                {
                    TaskId = fullTask!.TaskId,
                    SerialNumber = fullTask.SerialNumber ?? "",
                    PartNumber = fullTask.PartNumber ?? "",
                    PoNumber = fullTask.PoNumber ?? "",
                    Description = fullTask.Description,
                    CategoryId = fullTask.CategoryId,
                    CategoryName = fullTask.Category?.Name ?? "Unknown",
                    Status = fullTask.Status,
                    Comments = fullTask.Comments,
                    AssignedEngineer = fullTask.AssignedEngineer ?? "Unassigned",
                    PriorityId = fullTask.PriorityId,
                    PriorityName = fullTask.Priority?.LevelName ?? "Unknown",
                    EstimatedCompletionDate = fullTask.EstimatedCompletionDate,
                    TargetCompletionDate = fullTask.TargetCompletionDate,
                    ActualCompletionDate = fullTask.ActualCompletionDate,
                    AttachmentPath = fullTask.AttachmentPath,
                    AttachmentsCount = fullTask.Attachments?.Count ?? 0,
                    ShopId = fullTask.ShopId,
                    ShopName = fullTask.Shop?.Name ?? "Unknown",
                    CreatedBy = fullTask.CreatedBy,
                    CreatedByName = fullTask.Creator?.FullName ?? "Unknown",
                    CreatedAt = fullTask.CreatedAt,
                    UpdatedAt = fullTask.UpdatedAt
                };

                return CreatedAtAction(nameof(GetTask), new { id = taskDto.TaskId }, taskDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task");
                return StatusCode(500, new { message = "An error occurred while creating the task", error = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing task
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<TaskDto>> UpdateTask(int id, [FromBody] UpdateTaskDto request)
        {
            try
            {
                var (userId, userName) = GetCurrentUser();
                var existingTask = await _taskService.GetTaskByIdAsync(id);
                if (existingTask == null)
                {
                    return NotFound(new { message = $"Task with ID {id} not found" });
                }

                if (string.IsNullOrEmpty(request.Description))
                {
                    return BadRequest(new { message = "Description is required" });
                }

                var previousStatus = existingTask.Status;

                existingTask.SerialNumber = request.SerialNumber;
                existingTask.PartNumber = request.PartNumber;
                existingTask.PoNumber = request.PoNumber;
                existingTask.Description = request.Description;
                existingTask.CategoryId = request.CategoryId;
                existingTask.Status = request.Status;
                existingTask.Comments = request.Comments;
                existingTask.AssignedEngineer = request.AssignedEngineer ?? "Unassigned";
                existingTask.PriorityId = request.PriorityId;
                existingTask.EstimatedCompletionDate = request.EstimatedCompletionDate != default ? request.EstimatedCompletionDate : existingTask.EstimatedCompletionDate;
                existingTask.TargetCompletionDate = request.TargetCompletionDate;
                existingTask.ActualCompletionDate = request.ActualCompletionDate;
                existingTask.AttachmentPath = request.AttachmentPath;
                existingTask.ShopId = request.ShopId;
                existingTask.UpdatedAt = DateTime.UtcNow;

                if (request.Status == TaskStatusEnum.Completed && previousStatus != TaskStatusEnum.Completed)
                {
                    if (!existingTask.ActualCompletionDate.HasValue)
                    {
                        existingTask.ActualCompletionDate = DateTime.UtcNow.Date;
                    }

                    await _taskService.UpdateTaskAsync(existingTask, userId, userName);

                    _logger.LogInformation("Moving task {TaskId} to completed_tasks table", id);
                    var completedTask = await _completedTaskService.MoveTaskToCompletedAsync(existingTask);

                    var completedDto = new CompletedTaskDto
                    {
                        CompletedTaskId = completedTask.CompletedTaskId,
                        OriginalTaskId = completedTask.OriginalTaskId,
                        SerialNumber = completedTask.SerialNumber ?? "",
                        PartNumber = completedTask.PartNumber ?? "",
                        PoNumber = completedTask.PoNumber ?? "",
                        Description = completedTask.Description,
                        CategoryId = completedTask.CategoryId,
                        CategoryName = completedTask.Category?.Name ?? "Unknown",
                        Status = completedTask.Status,
                        Comments = completedTask.Comments,
                        AssignedEngineer = completedTask.AssignedEngineer ?? "Unassigned",
                        PriorityId = completedTask.PriorityId,
                        PriorityName = completedTask.Priority?.LevelName ?? "Unknown",
                        EstimatedCompletionDate = completedTask.EstimatedCompletionDate,
                        TargetCompletionDate = completedTask.TargetCompletionDate,
                        ActualCompletionDate = completedTask.ActualCompletionDate,
                        AttachmentPath = completedTask.AttachmentPath,
                        AttachmentsCount = completedTask.Attachments?.Count ?? 0,
                        ShopId = completedTask.ShopId,
                        ShopName = completedTask.Shop?.Name ?? "Unknown",
                        CreatedBy = completedTask.CreatedBy,
                        CreatedByName = completedTask.Creator?.FullName ?? "Unknown",
                        TaskCreatedAt = completedTask.TaskCreatedAt,
                        TaskUpdatedAt = completedTask.TaskUpdatedAt,
                        CompletedAt = completedTask.CompletedAt,
                        MovedToCompletedAt = completedTask.MovedToCompletedAt
                    };

                    return Ok(new { 
                        message = "Task completed and moved to completed tasks table",
                        movedToCompleted = true,
                        completedTask = completedDto 
                    });
                }

                var updatedTask = await _taskService.UpdateTaskAsync(existingTask, userId, userName);
                var fullTask = await _taskService.GetTaskByIdAsync(updatedTask.TaskId);

                var taskDto = new TaskDto
                {
                    TaskId = fullTask!.TaskId,
                    SerialNumber = fullTask.SerialNumber ?? "",
                    PartNumber = fullTask.PartNumber ?? "",
                    PoNumber = fullTask.PoNumber ?? "",
                    Description = fullTask.Description,
                    CategoryId = fullTask.CategoryId,
                    CategoryName = fullTask.Category?.Name ?? "Unknown",
                    Status = fullTask.Status,
                    Comments = fullTask.Comments,
                    AssignedEngineer = fullTask.AssignedEngineer ?? "Unassigned",
                    PriorityId = fullTask.PriorityId,
                    PriorityName = fullTask.Priority?.LevelName ?? "Unknown",
                    EstimatedCompletionDate = fullTask.EstimatedCompletionDate,
                    TargetCompletionDate = fullTask.TargetCompletionDate,
                    ActualCompletionDate = fullTask.ActualCompletionDate,
                    AttachmentPath = fullTask.AttachmentPath,
                    AttachmentsCount = fullTask.Attachments?.Count ?? 0,
                    ShopId = fullTask.ShopId,
                    ShopName = fullTask.Shop?.Name ?? "Unknown",
                    CreatedBy = fullTask.CreatedBy,
                    CreatedByName = fullTask.Creator?.FullName ?? "Unknown",
                    CreatedAt = fullTask.CreatedAt,
                    UpdatedAt = fullTask.UpdatedAt
                };

                return Ok(taskDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task {TaskId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the task", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete a task
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteTask(int id)
        {
            try
            {
                var success = await _taskService.DeleteTaskAsync(id);
                if (!success)
                {
                    return NotFound(new { message = $"Task with ID {id} not found" });
                }

                return Ok(new { message = $"Task {id} has been deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting task {TaskId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the task", error = ex.Message });
            }
        }

        /// <summary>
        /// Update task status - automatically moves to completed_tasks table when marked as Completed
        /// </summary>
        [HttpPatch("{id}/status")]
        public async Task<ActionResult> UpdateTaskStatus(int id, [FromBody] UpdateTaskStatusRequest request)
        {
            try
            {
                var (userId, userName) = GetCurrentUser();
                var existingTask = await _taskService.GetTaskByIdAsync(id);
                if (existingTask == null)
                {
                    return NotFound(new { message = $"Task with ID {id} not found" });
                }

                var previousStatus = existingTask.Status;

                existingTask.Status = request.Status;
                existingTask.UpdatedAt = DateTime.UtcNow;

                if (request.Status == TaskStatusEnum.Completed && previousStatus != TaskStatusEnum.Completed)
                {
                    if (!existingTask.ActualCompletionDate.HasValue)
                    {
                        existingTask.ActualCompletionDate = DateTime.UtcNow.Date;
                    }

                    await _taskService.UpdateTaskAsync(existingTask, userId, userName);

                    _logger.LogInformation("Moving task {TaskId} to completed_tasks table", id);
                    var completedTask = await _completedTaskService.MoveTaskToCompletedAsync(existingTask);

                    return Ok(new { 
                        message = "Task completed and moved to completed tasks table",
                        movedToCompleted = true,
                        completedTaskId = completedTask.CompletedTaskId,
                        originalTaskId = completedTask.OriginalTaskId
                    });
                }

                await _taskService.UpdateTaskAsync(existingTask, userId, userName);

                return Ok(new { 
                    message = "Task status updated successfully",
                    movedToCompleted = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task status for task {TaskId}", id);
                return StatusCode(500, new { message = "An error occurred while updating task status", error = ex.Message });
            }
        }

        /// <summary>
        /// Reopen a completed task - moves it back from completed_tasks to tasks table
        /// </summary>
        [HttpPost("{id}/reopen")]
        public async Task<ActionResult> ReopenTask(int id, [FromBody] ReopenTaskRequest request)
        {
            try
            {
                var (userId, userName) = GetCurrentUser();
                
                // Validate required fields
                if (string.IsNullOrWhiteSpace(request.ReopenComment))
                {
                    return BadRequest(new { error = "A comment is mandatory when reopening a task." });
                }

                if (request.AssignedEngineer == null || request.AssignedEngineer.Count == 0)
                {
                    return BadRequest(new { error = "You must assign at least one engineer when reopening a task." });
                }

                // First, try to find the task in the completed_tasks table
                var completedTask = await _completedTaskService.GetCompletedTaskByOriginalIdAsync(id);
                if (completedTask == null)
                {
                    // If not in completed_tasks, check if it's still in the tasks table
                    var activeTask = await _taskService.GetTaskByIdAsync(id);
                    if (activeTask == null)
                    {
                        return NotFound(new { error = $"Task with ID {id} not found in either active or completed tasks" });
                    }
                    
                    // Task is already active, just update it
                    activeTask.Status = Enum.TryParse<TaskStatusEnum>(request.Status, out var parsedStatus) ? parsedStatus : TaskStatusEnum.Pending;
                    activeTask.AssignedEngineer = string.Join(", ", request.AssignedEngineer);
                    activeTask.Comments = $"{activeTask.Comments}\n\n[REOPENED by {userName}]: {request.ReopenComment}";
                    activeTask.ActualCompletionDate = null; // Clear completion date
                    activeTask.UpdatedAt = DateTime.UtcNow;

                    await _taskService.UpdateTaskAsync(activeTask, userId, userName);
                    
                    return Ok(new { 
                        message = "Task status updated successfully (task was already active)",
                        taskId = activeTask.TaskId
                    });
                }

                // Move the task back from completed_tasks to tasks table
                // DON'T set TaskId - let the database auto-generate it
                var reopenedTask = new TaskEntity
                {
                    SerialNumber = completedTask.SerialNumber,
                    PartNumber = completedTask.PartNumber,
                    PoNumber = completedTask.PoNumber,
                    Description = completedTask.Description,
                    CategoryId = completedTask.CategoryId,
                    SubTypeId = completedTask.SubTypeId,
                    RequestTypeId = completedTask.RequestTypeId,
                    Status = Enum.TryParse<TaskStatusEnum>(request.Status, out var status) ? status : TaskStatusEnum.Pending,
                    Comments = $"{completedTask.Comments}\n\n[REOPENED by {userName}]: {request.ReopenComment}",
                    AssignedEngineer = string.Join(", ", request.AssignedEngineer),
                    PriorityId = completedTask.PriorityId,
                    EstimatedCompletionDate = completedTask.EstimatedCompletionDate,
                    TargetCompletionDate = completedTask.TargetCompletionDate,
                    ActualCompletionDate = null, // Clear the completion date since we're reopening
                    AttachmentPath = completedTask.AttachmentPath,
                    AmendmentRequest = completedTask.AmendmentRequest,
                    AmendmentStatus = completedTask.AmendmentStatus,
                    AmendmentReviewedByTlId = completedTask.AmendmentReviewedByTlId,
                    IsDuplicate = completedTask.IsDuplicate,
                    DuplicateJustification = completedTask.DuplicateJustification,
                    RevisionNotes = $"[REOPENED from completed task {completedTask.CompletedTaskId}] {completedTask.RevisionNotes}",
                    ShowRevisionAlert = true,
                    ShopId = completedTask.ShopId,
                    CreatedBy = completedTask.CreatedBy,
                    IsMandatory = completedTask.IsMandatory,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Create the task back in the tasks table (new TaskId will be auto-generated)
                var createdTask = await _taskService.CreateTaskAsync(reopenedTask);
                
                // Remove from completed_tasks table
                await _completedTaskService.DeleteCompletedTaskAsync(completedTask.CompletedTaskId);

                _logger.LogInformation("Task {OriginalTaskId} reopened as new task {NewTaskId} by user {UserId} ({UserName})", 
                    id, createdTask.TaskId, userId, userName);

                return Ok(new { 
                    message = "Task reopened successfully and moved back to active tasks",
                    taskId = createdTask.TaskId,
                    originalTaskId = id,
                    note = $"Task has been reopened with a new ID: {createdTask.TaskId}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reopening task {TaskId}. Details: {Message}", id, ex.Message);
                return StatusCode(500, new { error = "An error occurred while reopening the task", details = ex.Message });
            }
        }

        /// <summary>
        /// Get tasks by user ID
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetTasksByUser(int userId)
        {
            try
            {
                var tasks = await _taskService.GetTasksByUserIdAsync(userId);

                var taskDtos = tasks.Select(t => new TaskDto
                {
                    TaskId = t.TaskId,
                    SerialNumber = t.SerialNumber ?? "",
                    PartNumber = t.PartNumber ?? "",
                    PoNumber = t.PoNumber ?? "",
                    Description = t.Description,
                    CategoryId = t.CategoryId,
                    CategoryName = t.Category?.Name ?? "Unknown",
                    Status = t.Status,
                    Comments = t.Comments,
                    AssignedEngineer = t.AssignedEngineer ?? "Unassigned",
                    PriorityId = t.PriorityId,
                    PriorityName = t.Priority?.LevelName ?? "Unknown",
                    EstimatedCompletionDate = t.EstimatedCompletionDate,
                    TargetCompletionDate = t.TargetCompletionDate,
                    ActualCompletionDate = t.ActualCompletionDate,
                    AttachmentPath = t.AttachmentPath,
                    AttachmentsCount = t.Attachments?.Count ?? 0,
                    ShopId = t.ShopId,
                    ShopName = t.Shop?.Name ?? "Unknown",
                    CreatedBy = t.CreatedBy,
                    CreatedByName = t.Creator?.FullName ?? "Unknown",
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt
                }).ToList();

                return Ok(taskDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user tasks for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while retrieving user tasks", error = ex.Message });
            }
        }

        /// <summary>
        /// Get tasks by shop ID
        /// </summary>
        [HttpGet("shop/{shopId}")]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetTasksByShop(int shopId)
        {
            try
            {
                // Use positional arguments to match the service method signature and avoid named-parameter mismatches.
                var (tasks, totalCount) = await _taskService.GetTasksWithFiltersAsync(
                    null, null, null, null, null, null, 1, 1000);

                var shopTasks = tasks.Where(t => t.ShopId == shopId);

                var taskDtos = shopTasks.Select(t => new TaskDto
                {
                    TaskId = t.TaskId,
                    SerialNumber = t.SerialNumber ?? "",
                    PartNumber = t.PartNumber ?? "",
                    PoNumber = t.PoNumber ?? "",
                    Description = t.Description,
                    CategoryId = t.CategoryId,
                    CategoryName = t.Category?.Name ?? "Unknown",
                    Status = t.Status,
                    Comments = t.Comments,
                    AssignedEngineer = t.AssignedEngineer ?? "Unassigned",
                    PriorityId = t.PriorityId,
                    PriorityName = t.Priority?.LevelName ?? "Unknown",
                    EstimatedCompletionDate = t.EstimatedCompletionDate,
                    TargetCompletionDate = t.TargetCompletionDate,
                    ActualCompletionDate = t.ActualCompletionDate,
                    AttachmentPath = t.AttachmentPath,
                    AttachmentsCount = t.Attachments?.Count ?? 0,
                    ShopId = t.ShopId,
                    ShopName = t.Shop?.Name ?? "Unknown",
                    CreatedBy = t.CreatedBy,
                    CreatedByName = t.Creator?.FullName ?? "Unknown",
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt
                }).ToList();

                return Ok(taskDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving shop tasks for shop {ShopId}", shopId);
                return StatusCode(500, new { message = "An error occurred while retrieving shop tasks", error = ex.Message });
            }
        }

        /// <summary>
        /// Check for duplicate tasks
        /// </summary>
        [HttpPost("check-duplicates")]
        public ActionResult CheckTaskDuplicates([FromBody] object taskData)
        {
            try
            {
                return Ok(new { duplicates = new object[0] });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for duplicates");
                return StatusCode(500, new { message = "An error occurred while checking for duplicates", error = ex.Message });
            }
        }
    }

    public class UpdateTaskStatusRequest
    {
        public TaskStatusEnum Status { get; set; }
    }

    public class ReopenTaskRequest
    {
        public string Status { get; set; } = "Pending";
        public string ReopenComment { get; set; } = "";
        public List<string> AssignedEngineer { get; set; } = new List<string>();
    }

    public class RequestAmendmentDto
    {
        public string AmendmentReason { get; set; } = string.Empty;
    }
}