using MediatR;

namespace MesTech.Application.Commands.ProcessBotReturnRequest;

public record ProcessBotReturnRequestCommand : IRequest
{
    public string CustomerPhone { get; init; } = string.Empty;
    public string OrderNumber { get; init; } = string.Empty;
    public string? ReturnReason { get; init; }
    public string RequestChannel { get; init; } = string.Empty;
    public Guid TenantId { get; init; }
}

public sealed class ProcessBotReturnRequestHandler : IRequestHandler<ProcessBotReturnRequestCommand>
{
    public Task Handle(ProcessBotReturnRequestCommand request, CancellationToken cancellationToken)
    {
        // Minimal handler — domain logic lives in consumer, to be migrated in future sprints
        return Task.CompletedTask;
    }
}
