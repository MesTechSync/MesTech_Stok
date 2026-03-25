using MediatR;
using MesTech.Domain.Entities.Billing;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Billing.Commands.ChangeSubscriptionPlan;

public sealed class ChangeSubscriptionPlanHandler
    : IRequestHandler<ChangeSubscriptionPlanCommand, ChangeSubscriptionPlanResult>
{
    private readonly ITenantSubscriptionRepository _subscriptionRepo;
    private readonly ISubscriptionPlanRepository _planRepo;
    private readonly IUnitOfWork _uow;

    public ChangeSubscriptionPlanHandler(
        ITenantSubscriptionRepository subscriptionRepo,
        ISubscriptionPlanRepository planRepo,
        IUnitOfWork uow)
    {
        _subscriptionRepo = subscriptionRepo;
        _planRepo = planRepo;
        _uow = uow;
    }

    public async Task<ChangeSubscriptionPlanResult> Handle(
        ChangeSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var subscription = await _subscriptionRepo
            .GetActiveByTenantIdAsync(request.TenantId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Aktif abonelik bulunamadi.");

        var currentPlan = await _planRepo
            .GetByIdAsync(subscription.PlanId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Mevcut plan bulunamadi.");

        var newPlan = await _planRepo
            .GetByIdAsync(request.NewPlanId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Hedef plan bulunamadi: {request.NewPlanId}");

        if (subscription.PlanId == request.NewPlanId)
            throw new InvalidOperationException("Zaten ayni planda.");

        var isUpgrade = newPlan.MonthlyPrice > currentPlan.MonthlyPrice;
        var period = request.NewPeriod ?? subscription.Period;

        // Prorated amount: kalan gün * (yeni - eski) günlük fiyat
        var daysRemaining = subscription.NextBillingDate.HasValue
            ? Math.Max(0, (subscription.NextBillingDate.Value - DateTime.UtcNow).Days)
            : 0;

        var currentDaily = GetDailyRate(currentPlan, subscription.Period);
        var newDaily = GetDailyRate(newPlan, period);
        var proratedAmount = Math.Max(0, (newDaily - currentDaily) * daysRemaining);

        subscription.ChangePlan(request.NewPlanId, period);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ChangeSubscriptionPlanResult
        {
            SubscriptionId = subscription.Id,
            PreviousPlanName = currentPlan.Name,
            NewPlanName = newPlan.Name,
            ProratedAmount = Math.Round(proratedAmount, 2),
            NextBillingDate = subscription.NextBillingDate ?? DateTime.UtcNow,
            IsUpgrade = isUpgrade
        };
    }

    private static decimal GetDailyRate(SubscriptionPlan plan, BillingPeriod period)
        => period == BillingPeriod.Annual
            ? plan.AnnualPrice / 365m
            : plan.MonthlyPrice / 30m;
}
