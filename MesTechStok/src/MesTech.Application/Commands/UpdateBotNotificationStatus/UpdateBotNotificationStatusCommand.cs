using MediatR;

namespace MesTech.Application.Commands.UpdateBotNotificationStatus;

public record UpdateBotNotificationStatusCommand : IRequest
{
    public string Channel { get; init; } = string.Empty;
    public string Recipient { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid TenantId { get; init; }
}

public class UpdateBotNotificationStatusHandler : IRequestHandler<UpdateBotNotificationStatusCommand>
{
    public Task Handle(UpdateBotNotificationStatusCommand request, CancellationToken cancellationToken)
    {
        // Minimal handler — domain logic lives in consumer, to be migrated in future sprints
        return Task.CompletedTask;
    }
}
