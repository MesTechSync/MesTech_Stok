using MediatR;

namespace MesTech.Application.Commands.ApproveAccountingEntry;

public record ApproveAccountingEntryCommand : IRequest
{
    public Guid DocumentId { get; init; }
    public string ApprovedBy { get; init; } = string.Empty;
    public string ApprovalSource { get; init; } = string.Empty;
    public Guid? JournalEntryId { get; init; }
    public Guid TenantId { get; init; }
}

public sealed class ApproveAccountingEntryHandler : IRequestHandler<ApproveAccountingEntryCommand>
{
    public Task Handle(ApproveAccountingEntryCommand request, CancellationToken cancellationToken)
    {
        // Minimal handler — domain logic lives in consumer, to be migrated in future sprints
        return Task.CompletedTask;
    }
}
