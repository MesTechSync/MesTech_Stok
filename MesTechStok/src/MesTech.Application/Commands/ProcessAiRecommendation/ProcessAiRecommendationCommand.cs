using MediatR;

namespace MesTech.Application.Commands.ProcessAiRecommendation;

public record ProcessAiRecommendationCommand : IRequest
{
    public string RecommendationType { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? ActionUrl { get; init; }
    public string Priority { get; init; } = string.Empty;
    public Guid TenantId { get; init; }
}

public sealed class ProcessAiRecommendationHandler : IRequestHandler<ProcessAiRecommendationCommand>
{
    public Task Handle(ProcessAiRecommendationCommand request, CancellationToken cancellationToken)
    {
        // Minimal handler — domain logic lives in consumer, to be migrated in future sprints
        return Task.CompletedTask;
    }
}
