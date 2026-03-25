using FluentAssertions;
using MesTech.Application.DTOs.Fulfillment;
using MesTech.Application.Features.Accounting.Commands.CreateFixedAsset;
using MesTech.Application.Features.Accounting.Commands.CreatePlatformCommissionRate;
using MesTech.Application.Features.Billing.Commands.CancelSubscription;
using MesTech.Application.Features.Billing.Commands.CreateSubscription;
using MesTech.Application.Features.Finance.Commands.CreateExpense;
using MesTech.Application.Features.Fulfillment.Commands.CreateInboundShipment;
using MesTech.Application.Features.Reporting.Commands.CreateSavedReport;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Entities.Billing;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Tests for CreateFixedAsset, CreateInboundShipment, CreatePlatformCommissionRate,
/// CreateSavedReport, CreateSubscription, CancelSubscription, and Finance.CreateExpense handlers.
/// </summary>
[Trait("Category", "Unit")]
public class CreateMiscCommandTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    // ═══════ Finance.CreateExpenseHandler ═══════

    [Fact]
    public async Task CreateFinanceExpense_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IFinanceExpenseRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new CreateExpenseHandler(repo.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CreateFinanceExpense_ValidRequest_ReturnsGuid()
    {
        var repo = new Mock<IFinanceExpenseRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new CreateExpenseHandler(repo.Object, uow.Object);
        var cmd = new CreateExpenseCommand(
            _tenantId, "Office Supplies", 250m, ExpenseCategory.Software,
            DateTime.UtcNow);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeEmpty();
        repo.Verify(r => r.AddAsync(It.IsAny<MesTech.Domain.Entities.Finance.FinanceExpense>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════ CreateFixedAssetHandler ═══════

    [Fact]
    public async Task CreateFixedAsset_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IFixedAssetRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new CreateFixedAssetHandler(repo.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CreateFixedAsset_ValidRequest_ReturnsGuid()
    {
        var repo = new Mock<IFixedAssetRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new CreateFixedAssetHandler(repo.Object, uow.Object);
        var cmd = new CreateFixedAssetCommand(
            _tenantId, "CNC Machine", "253", 50000m, DateTime.UtcNow, 5,
            DepreciationMethod.StraightLine, "Factory equipment");

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeEmpty();
        repo.Verify(r => r.AddAsync(It.IsAny<MesTech.Domain.Accounting.Entities.FixedAsset>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════ CreateInboundShipmentHandler ═══════

    [Fact]
    public async Task CreateInboundShipment_NoProvider_ThrowsInvalidOperationException()
    {
        var factory = new Mock<IFulfillmentProviderFactory>();
        var logger = new Mock<ILogger<CreateInboundShipmentHandler>>();
        factory.Setup(f => f.Resolve(It.IsAny<FulfillmentCenter>())).Returns((IFulfillmentProvider?)null);

        var sut = new CreateInboundShipmentHandler(factory.Object, logger.Object);
        var cmd = new CreateInboundShipmentCommand(
            FulfillmentCenter.AmazonFBA, "Shipment-1",
            new List<InboundItem>());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task CreateInboundShipment_ValidProvider_ReturnsResult()
    {
        var factory = new Mock<IFulfillmentProviderFactory>();
        var logger = new Mock<ILogger<CreateInboundShipmentHandler>>();
        var provider = new Mock<IFulfillmentProvider>();
        var expectedResult = new InboundResult(true, "SHIP-001");
        provider.Setup(p => p.CreateInboundShipmentAsync(It.IsAny<InboundShipmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);
        factory.Setup(f => f.Resolve(FulfillmentCenter.AmazonFBA)).Returns(provider.Object);

        var sut = new CreateInboundShipmentHandler(factory.Object, logger.Object);
        var cmd = new CreateInboundShipmentCommand(
            FulfillmentCenter.AmazonFBA, "Shipment-1",
            new List<InboundItem>());

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ShipmentId.Should().Be("SHIP-001");
    }

    // ═══════ CreatePlatformCommissionRateHandler ═══════

    [Fact]
    public async Task CreatePlatformCommissionRate_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IPlatformCommissionRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new CreatePlatformCommissionRateHandler(repo.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CreatePlatformCommissionRate_ValidRequest_ReturnsGuid()
    {
        var repo = new Mock<IPlatformCommissionRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new CreatePlatformCommissionRateHandler(repo.Object, uow.Object);
        var cmd = new CreatePlatformCommissionRateCommand(
            _tenantId, PlatformType.Trendyol, 12.5m);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeEmpty();
        repo.Verify(r => r.AddAsync(It.IsAny<MesTech.Domain.Entities.PlatformCommission>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════ CreateSavedReportHandler ═══════

    [Fact]
    public async Task CreateSavedReport_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<ISavedReportRepository>();
        var uow = new Mock<IUnitOfWork>();
        var logger = new Mock<ILogger<CreateSavedReportHandler>>();
        var sut = new CreateSavedReportHandler(repo.Object, uow.Object, logger.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CreateSavedReport_ValidRequest_ReturnsGuid()
    {
        var repo = new Mock<ISavedReportRepository>();
        var uow = new Mock<IUnitOfWork>();
        var logger = new Mock<ILogger<CreateSavedReportHandler>>();
        var sut = new CreateSavedReportHandler(repo.Object, uow.Object, logger.Object);
        var cmd = new CreateSavedReportCommand(
            _tenantId, "Monthly Sales", "Sales", "{}", Guid.NewGuid());

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeEmpty();
        repo.Verify(r => r.AddAsync(It.IsAny<MesTech.Domain.Entities.Reporting.SavedReport>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════ CreateSubscriptionHandler ═══════

    [Fact]
    public async Task CreateSubscription_NullRequest_ThrowsArgumentNullException()
    {
        var subRepo = new Mock<ITenantSubscriptionRepository>();
        var planRepo = new Mock<ISubscriptionPlanRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new CreateSubscriptionHandler(subRepo.Object, planRepo.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CreateSubscription_PlanNotFound_ThrowsInvalidOperationException()
    {
        var subRepo = new Mock<ITenantSubscriptionRepository>();
        var planRepo = new Mock<ISubscriptionPlanRepository>();
        var uow = new Mock<IUnitOfWork>();
        planRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan?)null);

        var sut = new CreateSubscriptionHandler(subRepo.Object, planRepo.Object, uow.Object);
        var cmd = new CreateSubscriptionCommand(_tenantId, Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task CreateSubscription_AlreadyActive_ThrowsInvalidOperationException()
    {
        var subRepo = new Mock<ITenantSubscriptionRepository>();
        var planRepo = new Mock<ISubscriptionPlanRepository>();
        var uow = new Mock<IUnitOfWork>();
        var plan = SubscriptionPlan.Create("Pro", 99m, 990m, 5, 1000, 5);
        var planId = plan.Id;
        planRepo.Setup(r => r.GetByIdAsync(planId, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        var existing = TenantSubscription.StartTrial(_tenantId, planId, 14);
        subRepo.Setup(r => r.GetActiveByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var sut = new CreateSubscriptionHandler(subRepo.Object, planRepo.Object, uow.Object);
        var cmd = new CreateSubscriptionCommand(_tenantId, planId);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Handle(cmd, CancellationToken.None));
    }

    // ═══════ CancelSubscriptionHandler ═══════

    [Fact]
    public async Task CancelSubscription_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<ITenantSubscriptionRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new CancelSubscriptionHandler(repo.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CancelSubscription_NotFound_ThrowsInvalidOperationException()
    {
        var repo = new Mock<ITenantSubscriptionRepository>();
        var uow = new Mock<IUnitOfWork>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantSubscription?)null);

        var sut = new CancelSubscriptionHandler(repo.Object, uow.Object);
        var cmd = new CancelSubscriptionCommand(_tenantId, Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task CancelSubscription_WrongTenant_ThrowsInvalidOperationException()
    {
        var repo = new Mock<ITenantSubscriptionRepository>();
        var uow = new Mock<IUnitOfWork>();
        var planId = Guid.NewGuid();
        var subscription = TenantSubscription.StartTrial(Guid.NewGuid(), planId, 14); // different tenant
        var subId = subscription.Id;
        repo.Setup(r => r.GetByIdAsync(subId, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);

        var sut = new CancelSubscriptionHandler(repo.Object, uow.Object);
        var cmd = new CancelSubscriptionCommand(_tenantId, subId);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task CancelSubscription_ValidRequest_ReturnsUnitValue()
    {
        var repo = new Mock<ITenantSubscriptionRepository>();
        var uow = new Mock<IUnitOfWork>();
        var planId = Guid.NewGuid();
        var subscription = TenantSubscription.StartTrial(_tenantId, planId, 14);
        var subId = subscription.Id;
        repo.Setup(r => r.GetByIdAsync(subId, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);

        var sut = new CancelSubscriptionHandler(repo.Object, uow.Object);
        var cmd = new CancelSubscriptionCommand(_tenantId, subId, "No longer needed");

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().Be(MediatR.Unit.Value);
        repo.Verify(r => r.UpdateAsync(subscription, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
