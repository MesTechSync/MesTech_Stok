using MediatR;

namespace MesTech.Application.Commands.ProcessBotInvoiceRequest;

public record ProcessBotInvoiceRequestCommand : IRequest
{
    public string CustomerPhone { get; init; } = string.Empty;
    public string OrderNumber { get; init; } = string.Empty;
    public string RequestChannel { get; init; } = string.Empty;
    public Guid TenantId { get; init; }
}

public sealed class ProcessBotInvoiceRequestHandler : IRequestHandler<ProcessBotInvoiceRequestCommand>
{
    public Task Handle(ProcessBotInvoiceRequestCommand request, CancellationToken cancellationToken)
    {
        // Minimal handler — domain logic lives in consumer, to be migrated in future sprints
        return Task.CompletedTask;
    }
}
