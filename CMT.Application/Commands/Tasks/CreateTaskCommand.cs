using MediatR;
using Microsoft.EntityFrameworkCore;
using CMT.Application.DTOs;
using CMT.Application.Common;
using CMT.Application.Services;
using CMT.Domain.Data;
using CMT.Domain.Models;
using TaskEntity = CMT.Domain.Models.Task;
using TaskStatusEnum = CMT.Domain.Models.TaskStatus;

namespace CMT.Application.Commands.Tasks;

public class CreateTaskCommand : IRequest<OperationResult<TaskDto>>
{
    public string? SerialNumber { get; set; }
    public string? PartNumber { get; set; }
    public string? PoNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int? SubTypeId { get; set; }
    public int? RequestTypeId { get; set; }
    public string? Comments { get; set; }
    public string AssignedEngineer { get; set; } = "Unassigned";
    public int PriorityId { get; set; }
    public DateTime EstimatedCompletionDate { get; set; }
    public DateTime? TargetCompletionDate { get; set; }
    public string? AttachmentPath { get; set; }
    public bool IsDuplicate { get; set; } = false;
    public string? DuplicateJustification { get; set; }
    public int? ShopId { get; set; }
    public int CreatedBy { get; set; }
    public bool IsMandatory { get; set; } = false;
}

public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, OperationResult<TaskDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public CreateTaskCommandHandler(ApplicationDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async System.Threading.Tasks.Task<OperationResult<TaskDto>> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        var result = new OperationResult<TaskDto>();

        try
        {
            // Validate category exists
            var category = await _context.TaskCategories
                .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId, cancellationToken);
            
            if (category == null)
            {
                result.AddError(ErrorCode.NotFound, "Category not found");
                return result;
            }

            // Calculate target completion date if not provided
            if (!request.TargetCompletionDate.HasValue && request.CategoryId > 0)
            {
                var targetDays = await _context.TaskCategoryTargetDays
                    .Where(t => t.CategoryId == request.CategoryId)
                    .Select(t => t.TargetDays)
                    .FirstOrDefaultAsync(cancellationToken);

                if (targetDays > 0)
                {
                    request.TargetCompletionDate = request.EstimatedCompletionDate.AddDays(targetDays);
                }
            }

            // Create new task
            var task = new TaskEntity
            {
                SerialNumber = request.SerialNumber ?? string.Empty,
                PartNumber = request.PartNumber ?? string.Empty,
                PoNumber = request.PoNumber ?? string.Empty,
                Description = request.Description,
                CategoryId = request.CategoryId,
                SubTypeId = request.SubTypeId,
                RequestTypeId = request.RequestTypeId,
                Comments = request.Comments,
                AssignedEngineer = request.AssignedEngineer,
                PriorityId = request.PriorityId,
                EstimatedCompletionDate = request.EstimatedCompletionDate,
                TargetCompletionDate = request.TargetCompletionDate ?? request.EstimatedCompletionDate,
                AttachmentPath = request.AttachmentPath,
                IsDuplicate = request.IsDuplicate,
                DuplicateJustification = request.DuplicateJustification,
                ShopId = request.ShopId,
                CreatedBy = request.CreatedBy,
                IsMandatory = request.IsMandatory,
                Status = TaskStatusEnum.Pending
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync(cancellationToken);

            // Load related entities
            await _context.Entry(task)
                .Reference(t => t.Category)
                .LoadAsync(cancellationToken);

            await _context.Entry(task)
                .Reference(t => t.Priority)
                .LoadAsync(cancellationToken);

            await _context.Entry(task)
                .Reference(t => t.Creator)
                .LoadAsync(cancellationToken);

            await _context.Entry(task)
                .Reference(t => t.Shop)
                .LoadAsync(cancellationToken);

            // Send notifications to assigned engineers
            if (!string.IsNullOrEmpty(request.AssignedEngineer) && request.AssignedEngineer != "Unassigned")
            {
                var assignedEngineers = request.AssignedEngineer.Split(',').Select(e => e.Trim());
                await _notificationService.NotifyAssignedEngineersAsync(task.TaskId, assignedEngineers, cancellationToken);
            }

            // Map to DTO
            var taskDto = MapToDto(task);
            result.Payload = taskDto;

            return result;
        }
        catch (Exception ex)
        {
            result.AddError(ErrorCode.ServerError, ex.Message);
            return result;
        }
    }

    private static TaskDto MapToDto(TaskEntity task)
    {
        return new TaskDto
        {
            TaskId = task.TaskId,
            SerialNumber = task.SerialNumber,
            PartNumber = task.PartNumber,
            PoNumber = task.PoNumber,
            Description = task.Description,
            CategoryId = task.CategoryId,
            CategoryName = task.Category?.Name,
            SubTypeId = task.SubTypeId,
            RequestTypeId = task.RequestTypeId,
            Status = task.Status,
            Comments = task.Comments,
            AssignedEngineer = task.AssignedEngineer,
            PriorityId = task.PriorityId,
            PriorityName = task.Priority?.LevelName,
            EstimatedCompletionDate = task.EstimatedCompletionDate,
            TargetCompletionDate = task.TargetCompletionDate,
            ActualCompletionDate = task.ActualCompletionDate,
            AttachmentPath = task.AttachmentPath,
            ShopId = task.ShopId,
            ShopName = task.Shop?.Name,
            CreatedBy = task.CreatedBy,
            CreatedByName = task.Creator?.Username,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };
    }
}