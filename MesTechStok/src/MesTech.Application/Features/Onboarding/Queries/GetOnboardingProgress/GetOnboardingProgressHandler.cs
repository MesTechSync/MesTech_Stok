using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Onboarding.Queries.GetOnboardingProgress;

public class GetOnboardingProgressHandler : IRequestHandler<GetOnboardingProgressQuery, OnboardingProgressDto?>
{
    private readonly IOnboardingProgressRepository _repository;

    public GetOnboardingProgressHandler(IOnboardingProgressRepository repository)
        => _repository = repository;

    public async Task<OnboardingProgressDto?> Handle(GetOnboardingProgressQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var progress = await _repository.GetByTenantIdAsync(request.TenantId, cancellationToken);
        if (progress is null) return null;

        return new OnboardingProgressDto
        {
            Id = progress.Id,
            CurrentStep = progress.CurrentStep,
            CompletionPercentage = progress.CompletionPercentage,
            IsCompleted = progress.IsCompleted,
            StartedAt = progress.StartedAt,
            CompletedAt = progress.CompletedAt
        };
    }
}
