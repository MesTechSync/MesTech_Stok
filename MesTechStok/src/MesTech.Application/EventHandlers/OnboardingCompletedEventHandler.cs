using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

public interface IOnboardingCompletedEventHandler
{
    Task HandleAsync(Guid tenantId, Guid onboardingProgressId, DateTime startedAt, DateTime completedAt, CancellationToken ct);
}

public sealed class OnboardingCompletedEventHandler : IOnboardingCompletedEventHandler
{
    private readonly ILogger<OnboardingCompletedEventHandler> _logger;

    public OnboardingCompletedEventHandler(ILogger<OnboardingCompletedEventHandler> logger)
        => _logger = logger;

    public Task HandleAsync(Guid tenantId, Guid onboardingProgressId, DateTime startedAt, DateTime completedAt, CancellationToken ct)
    {
        var duration = completedAt - startedAt;
        _logger.LogInformation(
            "Onboarding tamamlandı — TenantId={TenantId}, Duration={Duration}, ProgressId={ProgressId}",
            tenantId, duration, onboardingProgressId);

        return Task.CompletedTask;
    }
}
