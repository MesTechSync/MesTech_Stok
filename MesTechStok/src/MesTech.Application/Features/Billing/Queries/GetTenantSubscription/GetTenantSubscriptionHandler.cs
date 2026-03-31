using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Billing.Queries.GetTenantSubscription;

public sealed class GetTenantSubscriptionHandler : IRequestHandler<GetTenantSubscriptionQuery, TenantSubscriptionDto?>
{
    private readonly ITenantSubscriptionRepository _repository;

    public GetTenantSubscriptionHandler(ITenantSubscriptionRepository repository)
        => _repository = repository;

    public async Task<TenantSubscriptionDto?> Handle(GetTenantSubscriptionQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var sub = await _repository.GetActiveByTenantIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
        if (sub is null) return null;

        return new TenantSubscriptionDto
        {
            Id = sub.Id,
            PlanId = sub.PlanId,
            PlanName = sub.Plan?.Name ?? "",
            Status = sub.Status,
            Period = sub.Period,
            StartDate = sub.StartDate,
            EndDate = sub.EndDate,
            TrialEndsAt = sub.TrialEndsAt,
            NextBillingDate = sub.NextBillingDate,
            IsExpired = sub.IsExpired
        };
    }
}
