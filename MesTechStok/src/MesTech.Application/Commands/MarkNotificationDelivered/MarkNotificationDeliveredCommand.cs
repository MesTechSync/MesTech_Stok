using MediatR;

namespace MesTech.Application.Commands.MarkNotificationDelivered;

public record MarkNotificationDeliveredCommand : IRequest
{
    public Guid TenantId { get; init; }
    public string Channel { get; init; } = string.Empty;
    public string Recipient { get; init; } = string.Empty;
    public string TemplateName { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}

public class MarkNotificationDeliveredHandler : IRequestHandler<MarkNotificationDeliveredCommand>
{
    public Task Handle(MarkNotificationDeliveredCommand request, CancellationToken cancellationToken)
    {
        // Minimal handler — domain logic lives in consumer, to be migrated in future sprints
        return Task.CompletedTask;
    }
}
