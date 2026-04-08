using FluentAssertions;
using MesTech.Application.Features.Billing.Queries.GetSubscriptionUsage;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Billing;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Billing.Queries;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class GetSubscriptionUsageHandlerTests
{
    private readonly Mock<ITenantSubscriptionRepository> _subscriptionRepo = new();
    private readonly Mock<ISubscriptionPlanRepository> _planRepo = new();
    private readonly Mock<IStoreRepository> _storeRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();

    private readonly Guid _tenantId = Guid.NewGuid();

    private GetSubscriptionUsageHandler CreateSut() => new(
        _subscriptionRepo.Object, _planRepo.Object,
        _storeRepo.Object, _productRepo.Object, _userRepo.Object);

    [Fact]
    public async Task Handle_NoActiveSubscription_ReturnsNull()
    {
        _subscriptionRepo
            .Setup(r => r.GetActiveByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantSubscription?)null);

        var result = await CreateSut().Handle(
            new GetSubscriptionUsageQuery(_tenantId), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_PlanNotFound_ReturnsNull()
    {
        var subscription = TenantSubscription.StartTrial(_tenantId, Guid.NewGuid());
        _subscriptionRepo
            .Setup(r => r.GetActiveByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _planRepo
            .Setup(r => r.GetByIdAsync(subscription.PlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan?)null);

        var result = await CreateSut().Handle(
            new GetSubscriptionUsageQuery(_tenantId), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithinLimits_ReturnsCorrectUsageAndNotOverLimit()
    {
        // Arrange — plan: 5 stores, 10000 products, 5 users. Usage: 2/500/3
        var plan = SubscriptionPlan.Create("Pro", 799m, 7990m, 5, 10000, 5);
        var subscription = TenantSubscription.Activate(_tenantId, plan.Id, BillingPeriod.Monthly);

        _subscriptionRepo
            .Setup(r => r.GetActiveByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _planRepo
            .Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        _storeRepo.Setup(r => r.CountByTenantAsync(_tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _productRepo.Setup(r => r.CountByTenantAsync(_tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(500);
        _userRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { new(), new(), new() }.AsReadOnly());

        // Act
        var result = await CreateSut().Handle(
            new GetSubscriptionUsageQuery(_tenantId), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.PlanName.Should().Be("Pro");
        result.StoresUsed.Should().Be(2);
        result.StoresLimit.Should().Be(5);
        result.ProductsUsed.Should().Be(500);
        result.ProductsLimit.Should().Be(10000);
        result.UsersUsed.Should().Be(3);
        result.UsersLimit.Should().Be(5);
        result.IsOverLimit.Should().BeFalse();
        // Max usage: users 3/5 = 60%
        result.UsagePercent.Should().Be(60.0m);
    }

    [Fact]
    public async Task Handle_OverLimit_ReturnsIsOverLimitTrue()
    {
        // Arrange — plan: 1 store, 500 products, 1 user. Usage: 3/600/2 (all over)
        var plan = SubscriptionPlan.Create("Basic", 299m, 2990m, 1, 500, 1);
        var subscription = TenantSubscription.Activate(_tenantId, plan.Id, BillingPeriod.Monthly);

        _subscriptionRepo
            .Setup(r => r.GetActiveByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _planRepo
            .Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        _storeRepo.Setup(r => r.CountByTenantAsync(_tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(3);
        _productRepo.Setup(r => r.CountByTenantAsync(_tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(600);
        _userRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { new(), new() }.AsReadOnly());

        // Act
        var result = await CreateSut().Handle(
            new GetSubscriptionUsageQuery(_tenantId), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.IsOverLimit.Should().BeTrue();
        // Max usage: stores 3/1 = 300%
        result.UsagePercent.Should().Be(300.0m);
    }

    [Fact]
    public async Task Handle_ZeroLimits_UsagePercentIsZero()
    {
        // Edge case: plan with 0 limits → division by zero guard
        var plan = SubscriptionPlan.Create("Free", 0m, 0m, 0, 0, 0);
        var subscription = TenantSubscription.Activate(_tenantId, plan.Id, BillingPeriod.Monthly);

        _subscriptionRepo
            .Setup(r => r.GetActiveByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _planRepo
            .Setup(r => r.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        _storeRepo.Setup(r => r.CountByTenantAsync(_tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _productRepo.Setup(r => r.CountByTenantAsync(_tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(10);
        _userRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>().AsReadOnly());

        // Act
        var result = await CreateSut().Handle(
            new GetSubscriptionUsageQuery(_tenantId), CancellationToken.None);

        // Assert — no division by zero
        result.Should().NotBeNull();
        result!.UsagePercent.Should().Be(0m);
        // IsOverLimit: 1 > 0 stores → true, 10 > 0 products → true
        result.IsOverLimit.Should().BeTrue();
    }
}
