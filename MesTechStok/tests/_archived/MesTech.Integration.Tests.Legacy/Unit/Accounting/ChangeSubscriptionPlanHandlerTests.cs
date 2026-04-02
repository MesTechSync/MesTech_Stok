using FluentAssertions;
using MesTech.Application.Features.Billing.Commands.ChangeSubscriptionPlan;
using MesTech.Domain.Entities.Billing;
using MesTech.Domain.Interfaces;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

/// <summary>
/// ChangeSubscriptionPlanHandler: SaaS plan değişikliği.
/// Kritik iş kuralları:
///   - Aktif abonelik yoksa → InvalidOperationException
///   - Aynı plana geçiş → InvalidOperationException
///   - Upgrade: yeni plan fiyatı > eski plan fiyatı
///   - Prorated amount hesaplaması (kalan gün × fark)
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Handler")]
[Trait("Group", "BillingChain")]
public class ChangeSubscriptionPlanHandlerTests
{
    private readonly Mock<ITenantSubscriptionRepository> _subRepo = new();
    private readonly Mock<ISubscriptionPlanRepository> _planRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    public ChangeSubscriptionPlanHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private ChangeSubscriptionPlanHandler CreateHandler() =>
        new(_subRepo.Object, _planRepo.Object, _uow.Object);

    private SubscriptionPlan CreatePlan(string name, decimal monthly, decimal annual) =>
        SubscriptionPlan.Create(name, monthly, annual, maxStores: 5, maxProducts: 1000, maxUsers: 5);

    [Fact]
    public async Task Handle_UpgradePlan_ReturnsSuccessWithIsUpgradeTrue()
    {
        // Arrange
        var basicPlan = CreatePlan("Baslangic", 299m, 2990m);
        var proPlan = CreatePlan("Profesyonel", 799m, 7990m);
        var subscription = TenantSubscription.Activate(_tenantId, basicPlan.Id, BillingPeriod.Monthly);

        _subRepo.Setup(r => r.GetActiveByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _planRepo.Setup(r => r.GetByIdAsync(basicPlan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(basicPlan);
        _planRepo.Setup(r => r.GetByIdAsync(proPlan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(proPlan);

        var cmd = new ChangeSubscriptionPlanCommand(_tenantId, proPlan.Id);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsUpgrade.Should().BeTrue();
        result.PreviousPlanName.Should().Be("Baslangic");
        result.NewPlanName.Should().Be("Profesyonel");
        result.ProratedAmount.Should().BeGreaterThanOrEqualTo(0);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DowngradePlan_IsUpgradeFalse()
    {
        var proPlan = CreatePlan("Profesyonel", 799m, 7990m);
        var basicPlan = CreatePlan("Baslangic", 299m, 2990m);
        var subscription = TenantSubscription.Activate(_tenantId, proPlan.Id, BillingPeriod.Monthly);

        _subRepo.Setup(r => r.GetActiveByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _planRepo.Setup(r => r.GetByIdAsync(proPlan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(proPlan);
        _planRepo.Setup(r => r.GetByIdAsync(basicPlan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(basicPlan);

        var cmd = new ChangeSubscriptionPlanCommand(_tenantId, basicPlan.Id);
        var handler = CreateHandler();

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsUpgrade.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NoActiveSubscription_ThrowsInvalidOperation()
    {
        _subRepo.Setup(r => r.GetActiveByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantSubscription?)null);

        var cmd = new ChangeSubscriptionPlanCommand(_tenantId, Guid.NewGuid());
        var handler = CreateHandler();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SamePlan_ThrowsInvalidOperation()
    {
        var plan = CreatePlan("Baslangic", 299m, 2990m);
        var subscription = TenantSubscription.Activate(_tenantId, plan.Id, BillingPeriod.Monthly);

        _subRepo.Setup(r => r.GetActiveByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _planRepo.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var cmd = new ChangeSubscriptionPlanCommand(_tenantId, plan.Id);
        var handler = CreateHandler();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_TargetPlanNotFound_ThrowsInvalidOperation()
    {
        var plan = CreatePlan("Baslangic", 299m, 2990m);
        var subscription = TenantSubscription.Activate(_tenantId, plan.Id, BillingPeriod.Monthly);
        var missingPlanId = Guid.NewGuid();

        _subRepo.Setup(r => r.GetActiveByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _planRepo.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        _planRepo.Setup(r => r.GetByIdAsync(missingPlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan?)null);

        var cmd = new ChangeSubscriptionPlanCommand(_tenantId, missingPlanId);
        var handler = CreateHandler();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(cmd, CancellationToken.None));
    }
}
