using MediatR;
using MesTech.Domain.Entities.Billing;

namespace MesTech.Application.Features.Billing.Commands.CreateSubscription;

public record CreateSubscriptionCommand(
    Guid TenantId, Guid PlanId, BillingPeriod Period = BillingPeriod.Monthly,
    bool StartAsTrial = true
) : IRequest<Guid>;
