using MediatR;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<RejectAccountingEntryHandler> _logger;

    public RejectAccountingEntryHandler(ILogger<RejectAccountingEntryHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(RejectAccountingEntryCommand request, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "RejectAccountingEntry: Document {DocumentId} rejected by {RejectedBy} via {Source}. Reason: {Reason}",
            request.DocumentId, request.RejectedBy, request.RejectionSource, request.Reason ?? "belirtilmedi");

        return Task.CompletedTask;
    }
}
