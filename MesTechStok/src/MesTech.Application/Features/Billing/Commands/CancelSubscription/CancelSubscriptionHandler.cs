using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Billing.Commands.CancelSubscription;

public sealed class CancelSubscriptionHandler : IRequestHandler<CancelSubscriptionCommand, Unit>
{
    private readonly ITenantSubscriptionRepository _repository;
    private readonly IUnitOfWork _uow;

    public CancelSubscriptionHandler(ITenantSubscriptionRepository repo, IUnitOfWork uow)
        => (_repository, _uow) = (repo, uow);

    public async Task<Unit> Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var subscription = await _repository.GetByIdAsync(request.SubscriptionId, cancellationToken)
            ?? throw new InvalidOperationException($"Abonelik bulunamadi: {request.SubscriptionId}");

        if (subscription.TenantId != request.TenantId)
            throw new InvalidOperationException("Abonelik bu tenant'a ait degil.");

        subscription.Cancel(request.Reason);
        await _repository.UpdateAsync(subscription, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
