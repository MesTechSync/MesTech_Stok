using MediatR;

namespace MesTech.Application.Commands.CreateEInvoiceFromDraft;

public record CreateEInvoiceFromDraftCommand : IRequest
{
    public Guid OrderId { get; init; }
    public string SuggestedEttnNo { get; init; } = string.Empty;
    public decimal SuggestedTotal { get; init; }
    public Guid TenantId { get; init; }
}

public class CreateEInvoiceFromDraftHandler : IRequestHandler<CreateEInvoiceFromDraftCommand>
{
    public Task Handle(CreateEInvoiceFromDraftCommand request, CancellationToken cancellationToken)
    {
        // Minimal handler — domain logic lives in consumer, to be migrated in future sprints
        return Task.CompletedTask;
    }
}
