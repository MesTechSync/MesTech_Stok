using MediatR;

namespace MesTech.Application.Features.Billing.Commands.CancelSubscription;

public record CancelSubscriptionCommand(
    Guid TenantId, Guid SubscriptionId, string? Reason = null
) : IRequest<Unit>;
