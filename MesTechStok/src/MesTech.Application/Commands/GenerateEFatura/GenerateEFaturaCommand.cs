using MediatR;

namespace MesTech.Application.Commands.GenerateEFatura;

public record GenerateEFaturaCommand : IRequest
{
    public string BotUserId { get; init; } = string.Empty;
    public Guid? OrderId { get; init; }
    public string? BuyerVkn { get; init; }
    public Guid TenantId { get; init; }
}

public class GenerateEFaturaHandler : IRequestHandler<GenerateEFaturaCommand>
{
    public Task Handle(GenerateEFaturaCommand request, CancellationToken cancellationToken)
    {
        // Minimal handler — domain logic lives in consumer, to be migrated in future sprints
        return Task.CompletedTask;
    }
}
