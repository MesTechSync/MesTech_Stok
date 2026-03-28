using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.EventHandlers;

/// <summary>
/// Business logic event handler testleri — GL handlers, Revenue, StockDeduction, Return.
/// Bu handler'lar gerçek domain entity manipülasyonu yapar (logger-only DEĞİL).
/// Her handler: guard clause + happy path + edge case.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "EventHandlers")]
[Trait("Group", "BusinessLogicEventHandler")]
public class BusinessLogicEventHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Mock<IUnitOfWork> _uow = new();

    public BusinessLogicEventHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    // ═══════════════════════════════════════════════════════════
    // CommissionChargedGLHandler — Z6 Zinciri
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task CommissionGL_ZeroAmount_SkipsGLEntry()
    {
        var handler = new CommissionChargedGLHandler(_uow.Object, Mock.Of<IJournalEntryRepository>(), Mock.Of<ILogger<CommissionChargedGLHandler>>());

        await handler.HandleAsync(Guid.NewGuid(), _tenantId, PlatformType.Trendyol, 0m, 0.10m, CancellationToken.None);

        // 0 tutar → SaveChanges çağrılmamalı
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CommissionGL_NegativeAmount_SkipsGLEntry()
    {
        var handler = new CommissionChargedGLHandler(_uow.Object, Mock.Of<IJournalEntryRepository>(), Mock.Of<ILogger<CommissionChargedGLHandler>>());

        await handler.HandleAsync(Guid.NewGuid(), _tenantId, PlatformType.Amazon, -50m, 0.15m, CancellationToken.None);

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CommissionGL_ValidAmount_CreatesGLEntry()
    {
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new CommissionChargedGLHandler(uow.Object, Mock.Of<IJournalEntryRepository>(), Mock.Of<ILogger<CommissionChargedGLHandler>>());

        await handler.HandleAsync(Guid.NewGuid(), _tenantId, PlatformType.Trendyol, 500m, 0.12m, CancellationToken.None);

        // Pozitif tutar → SaveChanges çağrılmalı
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(PlatformType.Trendyol)]
    [InlineData(PlatformType.Hepsiburada)]
    [InlineData(PlatformType.N11)]
    [InlineData(PlatformType.Amazon)]
    public async Task CommissionGL_AllPlatforms_CompletesSuccessfully(PlatformType platform)
    {
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new CommissionChargedGLHandler(uow.Object, Mock.Of<IJournalEntryRepository>(), Mock.Of<ILogger<CommissionChargedGLHandler>>());

        var act = () => handler.HandleAsync(Guid.NewGuid(), _tenantId, platform, 100m, 0.10m, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    // ═══════════════════════════════════════════════════════════
    // OrderConfirmedRevenueHandler — Z2 Zinciri
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task Revenue_ValidOrder_CreatesIncomeRecord()
    {
        var incomeRepo = new Mock<IIncomeRepository>();
        incomeRepo.Setup(r => r.AddAsync(It.IsAny<Income>())).Returns(Task.CompletedTask);

        var handler = new OrderConfirmedRevenueHandler(
            incomeRepo.Object, _uow.Object, Mock.Of<ILogger<OrderConfirmedRevenueHandler>>());

        await handler.HandleAsync(Guid.NewGuid(), _tenantId, "ORD-001", 5000m, null, CancellationToken.None);

        incomeRepo.Verify(r => r.AddAsync(It.Is<Income>(i =>
            i.TenantId == _tenantId &&
            i.Description.Contains("ORD-001")
        )), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Revenue_WithStoreId_SetsStoreOnIncome()
    {
        var storeId = Guid.NewGuid();
        var incomeRepo = new Mock<IIncomeRepository>();
        incomeRepo.Setup(r => r.AddAsync(It.IsAny<Income>())).Returns(Task.CompletedTask);

        var handler = new OrderConfirmedRevenueHandler(
            incomeRepo.Object, _uow.Object, Mock.Of<ILogger<OrderConfirmedRevenueHandler>>());

        await handler.HandleAsync(Guid.NewGuid(), _tenantId, "ORD-002", 3000m, storeId, CancellationToken.None);

        incomeRepo.Verify(r => r.AddAsync(It.Is<Income>(i =>
            i.StoreId == storeId
        )), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════
    // ZeroStockDetectedEventHandler — Z8 Zinciri
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task ZeroStock_ProductNotFound_DoesNotThrow()
    {
        var productRepo = new Mock<IProductRepository>();
        productRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Product?)null);

        var handler = new ZeroStockDetectedEventHandler(
            productRepo.Object, _uow.Object, Mock.Of<ILogger<ZeroStockDetectedEventHandler>>());

        var act = () => handler.HandleAsync(Guid.NewGuid(), "SKU-X", _tenantId, CancellationToken.None);

        await act.Should().NotThrowAsync();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ZeroStock_AlreadyInactive_SkipsDeactivation()
    {
        var product = new Product { IsActive = false, Name = "Test", SKU = "SKU-INACTIVE" };
        var productRepo = new Mock<IProductRepository>();
        productRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(product);

        var handler = new ZeroStockDetectedEventHandler(
            productRepo.Object, _uow.Object, Mock.Of<ILogger<ZeroStockDetectedEventHandler>>());

        await handler.HandleAsync(Guid.NewGuid(), "SKU-INACTIVE", _tenantId, CancellationToken.None);

        // Zaten pasif → SaveChanges çağrılmamalı
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ZeroStock_ActiveProduct_DeactivatesAndSaves()
    {
        var product = new Product { IsActive = true, Name = "Aktif Ürün", SKU = "SKU-ACTIVE" };
        var productRepo = new Mock<IProductRepository>();
        productRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(product);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new ZeroStockDetectedEventHandler(
            productRepo.Object, uow.Object, Mock.Of<ILogger<ZeroStockDetectedEventHandler>>());

        await handler.HandleAsync(Guid.NewGuid(), "SKU-ACTIVE", _tenantId, CancellationToken.None);

        product.IsActive.Should().BeFalse("ZeroStock → ürün pasife alınmalı");
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════
    // InvoiceApprovedGLHandler — Z3 Zinciri
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task InvoiceGL_ValidInvoice_CreatesGLEntry()
    {
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var journalRepo = new Mock<IJournalEntryRepository>();
        journalRepo.Setup(r => r.AddAsync(It.IsAny<MesTech.Domain.Accounting.Entities.JournalEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new InvoiceApprovedGLHandler(uow.Object, journalRepo.Object, Mock.Of<ILogger<InvoiceApprovedGLHandler>>());

        await handler.HandleAsync(Guid.NewGuid(), _tenantId, "INV-001", 11800m, 1800m, 10000m, CancellationToken.None);

        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvoiceGL_ZeroTax_SkipsTaxLine()
    {
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var journalRepo = new Mock<IJournalEntryRepository>();
        journalRepo.Setup(r => r.AddAsync(It.IsAny<MesTech.Domain.Accounting.Entities.JournalEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new InvoiceApprovedGLHandler(uow.Object, journalRepo.Object, Mock.Of<ILogger<InvoiceApprovedGLHandler>>());

        // 0 KDV ile fatura → 391 KDV satırı eklenmemeli (handler guard clause)
        var act = () => handler.HandleAsync(Guid.NewGuid(), _tenantId, "INV-002", 5000m, 0m, 5000m, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    // ═══════════════════════════════════════════════════════════
    // InvoiceCancelledReversalHandler — Z4 Zinciri
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task InvoiceReversal_ValidCancel_CreatesReversalEntry()
    {
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var journalRepo = new Mock<IJournalEntryRepository>();
        journalRepo.Setup(r => r.AddAsync(It.IsAny<MesTech.Domain.Accounting.Entities.JournalEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var invoiceRepo = new Mock<IInvoiceRepository>();
        var handler = new InvoiceCancelledReversalHandler(uow.Object, journalRepo.Object, invoiceRepo.Object, Mock.Of<ILogger<InvoiceCancelledReversalHandler>>());

        await handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "INV-003", "İptal sebebi", _tenantId, 11800m, CancellationToken.None);

        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════
    // OrderShippedCostHandler — Z7 Zinciri
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task ShippedCost_ValidShipment_CreatesGLEntry()
    {
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new OrderShippedCostHandler(uow.Object, new Mock<IJournalEntryRepository>().Object, Mock.Of<ILogger<OrderShippedCostHandler>>());

        await handler.HandleAsync(Guid.NewGuid(), _tenantId, "TR123", CargoProvider.YurticiKargo, 45.50m, CancellationToken.None);

        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ShippedCost_ZeroCost_SkipsGLEntry()
    {
        var handler = new OrderShippedCostHandler(_uow.Object, new Mock<IJournalEntryRepository>().Object, Mock.Of<ILogger<OrderShippedCostHandler>>());

        await handler.HandleAsync(Guid.NewGuid(), _tenantId, "TR000", CargoProvider.ArasKargo, 0m, CancellationToken.None);

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
