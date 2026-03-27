using MediatR;

namespace MesTech.Application.Features.Crm.Commands.UpdateDealStage;

public record UpdateDealStageCommand(
    Guid DealId,
    Guid NewStageId,
    Guid TenantId) : IRequest<UpdateDealStageResult>;

public sealed class UpdateDealStageResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
}
