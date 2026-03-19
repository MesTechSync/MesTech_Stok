using MediatR;

namespace MesTech.Application.Features.Crm.Commands.CreateDeal;

public record CreateDealCommand(
    Guid TenantId,
    string Title,
    Guid PipelineId,
    Guid StageId,
    decimal Amount,
    Guid? CrmContactId = null,
    DateTime? ExpectedCloseDate = null,
    Guid? AssignedToUserId = null,
    Guid? StoreId = null
) : IRequest<Guid>;
