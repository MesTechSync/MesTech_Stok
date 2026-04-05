using FluentAssertions;
using MesTech.Application.Features.Billing.Queries.GetUserFeatures;
using MesTech.Domain.Entities.Billing;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Billing.Queries;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class GetUserFeaturesHandlerTests
{
    private readonly Mock<ITenantSubscriptionRepository> _repo = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private GetUserFeaturesHandler CreateSut() => new(_repo.Object);

    [Fact]
    public void Constructor_NullRepo_Throws()
    {
        var act = () => new GetUserFeaturesHandler(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_NoSubscription_ReturnsLightTier()
    {
        _repo.Setup(r => r.GetActiveByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantSubscription?)null);

        var result = await CreateSut().Handle(
            new GetUserFeaturesQuery(_tenantId), CancellationToken.None);

        result.Tier.Should().Be(SubscriptionTier.Light);
        result.PlanName.Should().Be("Baslangic");
        result.DaysRemaining.Should().Be(0);
        result.LockedFeatures.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ProPlan_ReturnsProTierWithFeatures()
    {
        var plan = SubscriptionPlan.Create("Profesyonel", 799m, 7990m, 5, 10000, 5);
        var sub = TenantSubscription.Activate(_tenantId, plan.Id, BillingPeriod.Monthly);
        // Hack: set navigation property via reflection since it's private
        typeof(TenantSubscription).GetProperty("Plan")!.SetValue(sub, plan);

        _repo.Setup(r => r.GetActiveByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sub);

        var result = await CreateSut().Handle(
            new GetUserFeaturesQuery(_tenantId), CancellationToken.None);

        result.Tier.Should().Be(SubscriptionTier.Pro);
        result.PlanName.Should().Be("Profesyonel");
        result.EnabledFeatures.Should().NotBeEmpty();
        result.MaxPlatforms.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Handle_UltraProPlan_EnablesAllFeatures()
    {
        var plan = SubscriptionPlan.Create("Kurumsal", 1999m, 19990m,
            int.MaxValue, int.MaxValue, int.MaxValue);
        var sub = TenantSubscription.Activate(_tenantId, plan.Id, BillingPeriod.Annual);
        typeof(TenantSubscription).GetProperty("Plan")!.SetValue(sub, plan);

        _repo.Setup(r => r.GetActiveByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sub);

        var result = await CreateSut().Handle(
            new GetUserFeaturesQuery(_tenantId), CancellationToken.None);

        result.Tier.Should().Be(SubscriptionTier.UltraPro);
        result.PlanName.Should().Be("Kurumsal");
        // UltraPro should enable AI, MESA, CustomApi etc
        result.EnabledFeatures.Should().Contain("AiRecommendations");
        result.EnabledFeatures.Should().Contain("MesaOsIntegration");
        result.LockedFeatures.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UnknownPlanName_DefaultsToLight()
    {
        var plan = SubscriptionPlan.Create("CustomPlan", 100m, 1000m, 2, 500, 2);
        var sub = TenantSubscription.Activate(_tenantId, plan.Id, BillingPeriod.Monthly);
        typeof(TenantSubscription).GetProperty("Plan")!.SetValue(sub, plan);

        _repo.Setup(r => r.GetActiveByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sub);

        var result = await CreateSut().Handle(
            new GetUserFeaturesQuery(_tenantId), CancellationToken.None);

        // Unknown plan name → default Light tier
        result.Tier.Should().Be(SubscriptionTier.Light);
    }
}
