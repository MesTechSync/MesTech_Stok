using MediatR;
using MesTech.Domain.Entities.Billing;

namespace MesTech.Application.Features.Billing.Commands.ChangeSubscriptionPlan;

public record ChangeSubscriptionPlanCommand(
    Guid TenantId,
    Guid NewPlanId,
    BillingPeriod? NewPeriod = null
) : IRequest<ChangeSubscriptionPlanResult>;

public sealed class ChangeSubscriptionPlanResult
{
    public Guid SubscriptionId { get; init; }
    public string PreviousPlanName { get; init; } = string.Empty;
    public string NewPlanName { get; init; } = string.Empty;
    public decimal ProratedAmount { get; init; }
    public DateTime NextBillingDate { get; init; }
    public bool IsUpgrade { get; init; }
}
