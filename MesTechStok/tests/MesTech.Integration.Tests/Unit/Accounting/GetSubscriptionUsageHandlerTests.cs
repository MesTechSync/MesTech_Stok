using FluentAssertions;
using MesTech.Application.Features.Billing.Queries.GetSubscriptionUsage;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Billing;
using MesTech.Domain.Interfaces;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

/// <summary>
/// GetSubscriptionUsageHandler: SaaS kullanım durumu sorgusu.
/// Kritik iş kuralları:
///   - IsOverLimit: store/product/user limitlerinden herhangi biri aşılmışsa true
///   - UsagePercent: en yüksek kullanım yüzdesini gösterir
///   - Abonelik yoksa null döner
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Handler")]
[Trait("Group", "BillingChain")]
public class GetSubscriptionUsageHandlerTests
{
    private readonly Mock<ITenantSubscriptionRepository> _subRepo = new();
    private readonly Mock<ISubscriptionPlanRepository> _planRepo = new();
    private readonly Mock<IStoreRepository> _storeRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private GetSubscriptionUsageHandler CreateHandler() =>
        new(_subRepo.Object, _planRepo.Object, _storeRepo.Object, _productRepo.Object, _userRepo.Object);

    private void SetupSubscription(SubscriptionPlan plan, int stores, int products, int users)
    {
        var subscription = TenantSubscription.Activate(_tenantId, plan.Id, BillingPeriod.Monthly);
        _subRepo.Setup(r => r.GetActiveByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _planRepo.Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        _storeRepo.Setup(r => r.CountByTenantAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stores);
        _productRepo.Setup(r => r.CountByTenantAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);
        _userRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Range(0, users).Select(_ => new User()).ToList());
    }

    [Fact]
    public async Task Handle_NormalUsage_ReturnsCorrectDto()
    {
        // Arrange — 2/5 store, 100/1000 product, 2/5 user
        var plan = SubscriptionPlan.Create("Profesyonel", 799m, 7990m,
            maxStores: 5, maxProducts: 1000, maxUsers: 5);
        SetupSubscription(plan, stores: 2, products: 100, users: 2);

        var handler = CreateHandler();
        var query = new GetSubscriptionUsageQuery(_tenantId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.PlanName.Should().Be("Profesyonel");
        result.StoresUsed.Should().Be(2);
        result.StoresLimit.Should().Be(5);
        result.ProductsUsed.Should().Be(100);
        result.ProductsLimit.Should().Be(1000);
        result.IsOverLimit.Should().BeFalse();
        result.UsagePercent.Should().Be(40.0m); // max(2/5, 100/1000, 2/5) = 40%
    }

    [Fact]
    public async Task Handle_OverLimit_IsOverLimitTrue()
    {
        // Arrange — 6/5 store → limit aşıldı
        var plan = SubscriptionPlan.Create("Baslangic", 299m, 2990m,
            maxStores: 5, maxProducts: 500, maxUsers: 1);
        SetupSubscription(plan, stores: 6, products: 100, users: 1);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetSubscriptionUsageQuery(_tenantId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.IsOverLimit.Should().BeTrue();
        result.UsagePercent.Should().BeGreaterThan(100m);
    }

    [Fact]
    public async Task Handle_NoSubscription_ReturnsNull()
    {
        _subRepo.Setup(r => r.GetActiveByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantSubscription?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetSubscriptionUsageQuery(_tenantId), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ZeroUsage_UsagePercentIsZero()
    {
        var plan = SubscriptionPlan.Create("Baslangic", 299m, 2990m,
            maxStores: 5, maxProducts: 500, maxUsers: 5);
        SetupSubscription(plan, stores: 0, products: 0, users: 0);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetSubscriptionUsageQuery(_tenantId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.UsagePercent.Should().Be(0m);
        result.IsOverLimit.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ExactlyAtLimit_NotOverLimit()
    {
        // Arrange — tam limitte
        var plan = SubscriptionPlan.Create("Baslangic", 299m, 2990m,
            maxStores: 2, maxProducts: 100, maxUsers: 1);
        SetupSubscription(plan, stores: 2, products: 100, users: 1);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetSubscriptionUsageQuery(_tenantId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.IsOverLimit.Should().BeFalse(); // tam limit aşım değil
        result.UsagePercent.Should().Be(100.0m);
    }
}
