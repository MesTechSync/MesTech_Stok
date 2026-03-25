using MediatR;

namespace MesTech.Application.Commands.RejectAccountingEntry;

public record RejectAccountingEntryCommand : IRequest
{
    public Guid DocumentId { get; init; }
    public string RejectedBy { get; init; } = string.Empty;
    public string RejectionSource { get; init; } = string.Empty;
    public string? Reason { get; init; }
    public Guid TenantId { get; init; }
}

public sealed class RejectAccountingEntryHandler : IRequestHandler<RejectAccountingEntryCommand>
{
    public Task Handle(RejectAccountingEntryCommand request, CancellationToken cancellationToken)
    {
        // Minimal handler — domain logic lives in consumer, to be migrated in future sprints
        return Task.CompletedTask;
    }
}
