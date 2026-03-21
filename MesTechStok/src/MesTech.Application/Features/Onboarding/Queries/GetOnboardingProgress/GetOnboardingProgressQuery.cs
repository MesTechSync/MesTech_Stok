using MediatR;
using MesTech.Domain.Entities.Onboarding;

namespace MesTech.Application.Features.Onboarding.Queries.GetOnboardingProgress;

public record GetOnboardingProgressQuery(Guid TenantId) : IRequest<OnboardingProgressDto?>;

public record OnboardingProgressDto
{
    public Guid Id { get; init; }
    public OnboardingStep CurrentStep { get; init; }
    public int CompletionPercentage { get; init; }
    public bool IsCompleted { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}
