using CMT.Application.DTOs;
using CMT.Domain.Models;

namespace CMT.Application.Services;

public interface IAmendmentService
{
    Task<ServiceResult<AmendmentDto>> RequestAmendmentAsync(int taskId, int requesterId, string reason, CancellationToken cancellationToken = default);
    Task<ServiceResult<AmendmentDto>> ReviewAmendmentAsync(int taskId, int reviewerId, AmendmentStatus decision, string reviewNotes, CancellationToken cancellationToken = default);
    Task<ServiceResult<AmendmentDto>> ApproveAmendmentAsync(int taskId, int approverId, string approvalNotes, CancellationToken cancellationToken = default);
    Task<ServiceResult<AmendmentDto>> RejectAmendmentAsync(int taskId, int rejectorId, string rejectionReason, CancellationToken cancellationToken = default);
    Task<ServiceResult<List<AmendmentDto>>> GetPendingAmendmentsAsync(int userId, CancellationToken cancellationToken = default);
    Task<ServiceResult<List<AmendmentDto>>> GetAmendmentHistoryAsync(int taskId, CancellationToken cancellationToken = default);
    Task<ServiceResult<AmendmentDto>> ForwardToDirectorAsync(int taskId, int teamLeaderId, string forwardingNotes, CancellationToken cancellationToken = default);
}

public class AmendmentDto
{
    public int TaskId { get; set; }
    public string TaskDescription { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public AmendmentStatus Status { get; set; }
    public string RequestReason { get; set; } = string.Empty;
    public string ReviewNotes { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public string ReviewerName { get; set; } = string.Empty;
    public int RequesterId { get; set; }
    public int? ReviewerId { get; set; }
    public bool RequiresDirectorApproval { get; set; }
}