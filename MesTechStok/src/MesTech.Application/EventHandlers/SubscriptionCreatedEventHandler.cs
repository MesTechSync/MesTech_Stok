using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

public interface ISubscriptionCreatedEventHandler
{
    Task HandleAsync(Guid tenantId, Guid subscriptionId, Guid planId, CancellationToken ct);
}

public class SubscriptionCreatedEventHandler : ISubscriptionCreatedEventHandler
{
    private readonly ILogger<SubscriptionCreatedEventHandler> _logger;

    public SubscriptionCreatedEventHandler(ILogger<SubscriptionCreatedEventHandler> logger)
        => _logger = logger;

    public Task HandleAsync(Guid tenantId, Guid subscriptionId, Guid planId, CancellationToken ct)
    {
        _logger.LogInformation(
            "Abonelik oluşturuldu — TenantId={TenantId}, SubscriptionId={SubscriptionId}, PlanId={PlanId}",
            tenantId, subscriptionId, planId);

        return Task.CompletedTask;
    }
}
