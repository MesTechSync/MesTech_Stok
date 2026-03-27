using MediatR;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Domain.ValueObjects;

namespace MesTech.Application.Features.Billing.Queries.GetUserFeatures;

public sealed class GetUserFeaturesHandler
    : IRequestHandler<GetUserFeaturesQuery, UserFeaturesResult>
{
    private readonly ITenantSubscriptionRepository _subscriptionRepo;

    public GetUserFeaturesHandler(ITenantSubscriptionRepository subscriptionRepo)
        => _subscriptionRepo = subscriptionRepo ?? throw new ArgumentNullException(nameof(subscriptionRepo));

    public async Task<UserFeaturesResult> Handle(
        GetUserFeaturesQuery request, CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionRepo
            .GetActiveByTenantIdAsync(request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        var tier = subscription?.Plan?.Name switch
        {
            "Profesyonel" or "Pro" => SubscriptionTier.Pro,
            "Kurumsal" or "UltraPro" => SubscriptionTier.UltraPro,
            _ => SubscriptionTier.Light
        };

        var matrix = FeatureMatrix.ForTier(tier);

        var allFeatures = new[]
        {
            "AiRecommendations", "MesaOsIntegration", "CustomApi", "WhiteLabel",
            "AdvancedReporting", "MultiWarehouse", "EInvoice", "DropshippingPool"
        };

        var enabled = allFeatures.Where(f => matrix.IsFeatureEnabled(f)).ToList();
        var locked = allFeatures.Where(f => !matrix.IsFeatureEnabled(f)).ToList();

        var daysRemaining = subscription?.EndDate is not null
            ? Math.Max(0, (int)(subscription.EndDate.Value - DateTime.UtcNow).TotalDays)
            : 0;

        return new UserFeaturesResult
        {
            Tier = tier,
            PlanName = subscription?.Plan?.Name ?? "Baslangic",
            MaxPlatforms = matrix.MaxPlatforms,
            MaxProducts = matrix.MaxProducts,
            MaxUsers = matrix.MaxUsers,
            EnabledFeatures = enabled,
            LockedFeatures = locked,
            DaysRemaining = daysRemaining
        };
    }
}
