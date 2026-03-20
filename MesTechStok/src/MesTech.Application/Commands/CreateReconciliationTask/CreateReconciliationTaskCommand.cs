using MediatR;

namespace MesTech.Application.Commands.CreateReconciliationTask;

public record CreateReconciliationTaskCommand : IRequest
{
    public Guid? SettlementBatchId { get; init; }
    public Guid? BankTransactionId { get; init; }
    public decimal Confidence { get; init; }
    public string? Rationale { get; init; }
    public Guid TenantId { get; init; }
}

public class CreateReconciliationTaskHandler : IRequestHandler<CreateReconciliationTaskCommand>
{
    public Task Handle(CreateReconciliationTaskCommand request, CancellationToken cancellationToken)
    {
        // Minimal handler — domain logic lives in consumer, to be migrated in future sprints
        return Task.CompletedTask;
    }
}
