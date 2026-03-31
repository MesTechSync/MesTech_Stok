using MediatR;
using MesTech.Domain.Entities.Billing;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Billing.Commands.CreateSubscription;

public sealed class CreateSubscriptionHandler : IRequestHandler<CreateSubscriptionCommand, Guid>
{
    private readonly ITenantSubscriptionRepository _subscriptionRepo;
    private readonly ISubscriptionPlanRepository _planRepo;
    private readonly IUnitOfWork _uow;

    public CreateSubscriptionHandler(
        ITenantSubscriptionRepository subscriptionRepo,
        ISubscriptionPlanRepository planRepo,
        IUnitOfWork uow)
    {
        _subscriptionRepo = subscriptionRepo;
        _planRepo = planRepo;
        _uow = uow;
    }

    public async Task<Guid> Handle(CreateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var plan = await _planRepo.GetByIdAsync(request.PlanId, cancellationToken)
            ?? throw new InvalidOperationException($"Plan bulunamadi: {request.PlanId}");

        // Mevcut aktif abonelik varsa engelle
        var existing = await _subscriptionRepo.GetActiveByTenantIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
            throw new InvalidOperationException("Tenant'in zaten aktif bir aboneligi var.");

        var subscription = request.StartAsTrial
            ? TenantSubscription.StartTrial(request.TenantId, request.PlanId, plan.TrialDays)
            : TenantSubscription.Activate(request.TenantId, request.PlanId, request.Period);

        await _subscriptionRepo.AddAsync(subscription, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return subscription.Id;
    }
}
