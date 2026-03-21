using MediatR;
using MesTech.Domain.Entities.Billing;

namespace MesTech.Application.Features.Billing.Queries.GetTenantSubscription;

public record GetTenantSubscriptionQuery(Guid TenantId) : IRequest<TenantSubscriptionDto?>;

public record TenantSubscriptionDto
{
    public Guid Id { get; init; }
    public Guid PlanId { get; init; }
    public string PlanName { get; init; } = string.Empty;
    public SubscriptionStatus Status { get; init; }
    public BillingPeriod Period { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public DateTime? TrialEndsAt { get; init; }
    public DateTime? NextBillingDate { get; init; }
    public bool IsExpired { get; init; }
}
