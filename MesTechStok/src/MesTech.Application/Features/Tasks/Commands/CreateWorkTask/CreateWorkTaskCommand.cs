using MediatR;
using MesTech.Domain.Enums;

namespace MesTech.Application.Features.Tasks.Commands.CreateWorkTask;

public record CreateWorkTaskCommand(
    Guid TenantId, string Title,
    TaskPriority Priority = TaskPriority.Normal,
    Guid? ProjectId = null, Guid? MilestoneId = null,
    Guid? AssignedToUserId = null, Guid? CreatedByUserId = null,
    DateTime? DueDate = null, int? EstimatedMinutes = null,
    Guid? OrderId = null, Guid? CrmContactId = null, Guid? ProductId = null
) : IRequest<Guid>;
