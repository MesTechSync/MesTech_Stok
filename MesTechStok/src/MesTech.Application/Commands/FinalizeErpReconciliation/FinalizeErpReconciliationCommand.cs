using MediatR;

namespace MesTech.Application.Commands.FinalizeErpReconciliation;

public record FinalizeErpReconciliationCommand : IRequest
{
    public string ErpProvider { get; init; } = string.Empty;
    public int ReconciledCount { get; init; }
    public int MismatchCount { get; init; }
    public Guid TenantId { get; init; }
}

public sealed class FinalizeErpReconciliationHandler : IRequestHandler<FinalizeErpReconciliationCommand>
{
    public Task Handle(FinalizeErpReconciliationCommand request, CancellationToken cancellationToken)
    {
        // Minimal handler — domain logic lives in consumer, to be migrated in future sprints
        return Task.CompletedTask;
    }
}
