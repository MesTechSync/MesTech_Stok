using MediatR;

namespace MesTech.Application.Features.Billing.Queries.GetSubscriptionUsage;

public record GetSubscriptionUsageQuery(Guid TenantId) : IRequest<SubscriptionUsageDto?>;

public sealed class SubscriptionUsageDto
{
    public string PlanName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime? TrialEndsAt { get; init; }
    public DateTime? NextBillingDate { get; init; }
    public int StoresUsed { get; init; }
    public int StoresLimit { get; init; }
    public int ProductsUsed { get; init; }
    public int ProductsLimit { get; init; }
    public int UsersUsed { get; init; }
    public int UsersLimit { get; init; }
    public decimal UsagePercent { get; init; }
    public bool IsOverLimit { get; init; }
}
