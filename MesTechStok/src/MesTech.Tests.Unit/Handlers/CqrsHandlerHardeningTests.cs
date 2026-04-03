using FluentAssertions;
using MediatR;
using MesTech.Application.Commands.AddStock;
using MesTech.Application.Commands.BulkUpdatePrice;
using MesTech.Application.Commands.BulkUpdateStock;
using MesTech.Application.Commands.CreateCariHesap;
using MesTech.Application.Commands.CreateProduct;
using MesTech.Application.Commands.CreateQuotation;
using MesTech.Application.Commands.DeleteProduct;
using MesTech.Application.Commands.PlaceOrder;
using MesTech.Application.Commands.RemoveStock;
using MesTech.Application.Commands.SyncPlatform;
using MesTech.Application.Commands.UpdateProduct;
using MesTech.Application.DTOs;
using MesTech.Application.DTOs.Shipping;
using MesTech.Application.Features.Accounting.Commands.CreateAccountingExpense;
using MesTech.Application.Features.Accounting.Commands.CreateChartOfAccount;
using MesTech.Application.Features.Accounting.Commands.CreateCounterparty;
using MesTech.Application.Features.Accounting.Commands.CreateJournalEntry;
using MesTech.Application.Features.Accounting.Commands.CreatePlatformCommissionRate;
using MesTech.Application.Features.Accounting.Commands.DeleteChartOfAccount;
using MesTech.Application.Features.Accounting.Commands.ImportBankStatement;
using MesTech.Application.Features.Accounting.Commands.ImportSettlement;
using MesTech.Application.Features.Accounting.Commands.RecordCommission;
using MesTech.Application.Features.Accounting.Commands.RunReconciliation;
using MesTech.Application.Features.Accounting.Commands.UpdateChartOfAccount;
using MesTech.Application.Features.Accounting.Commands.UpdatePlatformCommissionRate;
using MesTech.Application.Features.Dropshipping.Commands.CreateDropshipSupplier;
using MesTech.Application.Features.Shipping.Commands.AutoShipOrder;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Services;
using MesTech.Domain.Dropshipping.Entities;
using MesTech.Domain.Dropshipping.Enums;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Domain.Services;
using Moq;
using AccountingAccountType = MesTech.Domain.Accounting.Enums.AccountType;
using CounterpartyType = MesTech.Domain.Accounting.Enums.CounterpartyType;
using ExpenseSource = MesTech.Domain.Accounting.Enums.ExpenseSource;
using IAutoShipmentService = MesTech.Domain.Services.IAutoShipmentService;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// CQRS Handler Hardening Tests -- Null/Edge Guard + FluentValidation Audit.
/// Sprint 3 (DEV-H3): 50 tests across 20 critical handlers and 10 validators.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Sprint", "H3-Hardening")]
public class CqrsHandlerHardeningTests
{
    // ----------------------------------------------------------------
    // Shared Mocks
    // ----------------------------------------------------------------
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly Mock<IStockMovementRepository> _movementRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IIntegratorOrchestrator> _orchestratorMock = new();
    private readonly Mock<IJournalEntryRepository> _journalRepoMock = new();
    private readonly Mock<ISettlementBatchRepository> _settlementRepoMock = new();
    private readonly Mock<IBankTransactionRepository> _bankTxRepoMock = new();
    private readonly Mock<ICommissionRecordRepository> _commissionRepoMock = new();
    private readonly Mock<ICounterpartyRepository> _counterpartyRepoMock = new();
    private readonly Mock<IPersonalExpenseRepository> _expenseRepoMock = new();
    private readonly Mock<IQuotationRepository> _quotationRepoMock = new();
    private readonly Mock<ICariHesapRepository> _cariHesapRepoMock = new();
    private readonly Mock<IDropshipSupplierRepository> _dropshipSupplierRepoMock = new();
    private readonly Mock<ICargoProviderFactory> _cargoProviderFactoryMock = new();
    private readonly Mock<IAutoShipmentService> _autoShipmentServiceMock = new();
    private readonly Mock<IReconciliationScoringService> _scoringServiceMock = new();
    private readonly Mock<IReconciliationMatchRepository> _matchRepoMock = new();

    private readonly Guid _tenantId = Guid.NewGuid();

    // ================================================================
    // PART 1: TOP 20 CRITICAL HANDLERS -- NULL/EDGE GUARD TESTS (40)
    // ================================================================

    // ------------ 1. CreateProduct ------------

    [Fact]
    public async Task CreateProductHandler_NullSKU_HandlesGracefully()
    {
        _productRepoMock.Setup(r => r.GetBySKUAsync(It.IsAny<string>())).ReturnsAsync((Product?)null);

        var handler = new CreateProductHandler(_productRepoMock.Object, _uowMock.Object, Mock.Of<ITenantProvider>());
        var command = new CreateProductCommand(
            Name: "Test Product",
            SKU: null!,
            Barcode: null,
            PurchasePrice: 10m,
            SalePrice: 20m,
            CategoryId: Guid.NewGuid());

        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CreateProductHandler_ZeroPrices_ReturnsSuccess()
    {
        _productRepoMock.Setup(r => r.GetBySKUAsync(It.IsAny<string>())).ReturnsAsync((Product?)null);

        var handler = new CreateProductHandler(_productRepoMock.Object, _uowMock.Object, Mock.Of<ITenantProvider>());
        var command = new CreateProductCommand(
            Name: "Free Product",
            SKU: "FREE-001",
            Barcode: null,
            PurchasePrice: 0m,
            SalePrice: 0m,
            CategoryId: Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    // ------------ 2. PlaceOrder ------------

    [Fact]
    public async Task PlaceOrderHandler_NullCommand_ThrowsArgumentNullException()
    {
        var stockCalc = new StockCalculationService();
        var handler = new PlaceOrderHandler(
            _orderRepoMock.Object, _productRepoMock.Object,
            _uowMock.Object, stockCalc, new Mock<ITenantProvider>().Object);

        var act = () => handler.Handle(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task PlaceOrderHandler_EmptyItems_HandlesGracefully()
    {
        _productRepoMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var stockCalc = new StockCalculationService();
        var handler = new PlaceOrderHandler(
            _orderRepoMock.Object, _productRepoMock.Object,
            _uowMock.Object, stockCalc, new Mock<ITenantProvider>().Object);

        var command = new PlaceOrderCommand(
            CustomerId: Guid.NewGuid(),
            CustomerName: "Test Customer",
            CustomerEmail: "test@test.com",
            Notes: null,
            Items: Array.Empty<PlaceOrderItem>());

        // Empty items list -- should proceed without error
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    // ------------ 3. AddStock (UpdateStock) ------------

    [Fact]
    public async Task AddStockHandler_NonExistentProduct_ReturnsFailure()
    {
        _productRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Product?)null);

        var handler = new AddStockHandler(
            _productRepoMock.Object, _movementRepoMock.Object, _uowMock.Object, Mock.Of<ITenantProvider>());

        var command = new AddStockCommand(
            ProductId: Guid.NewGuid(),
            Quantity: 10,
            UnitCost: 5.0m);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task AddStockHandler_ZeroQuantity_ExecutesWithoutError()
    {
        var product = new Product { Name = "Test", SKU = "T1", Stock = 10 };
        _productRepoMock.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var handler = new AddStockHandler(
            _productRepoMock.Object, _movementRepoMock.Object, _uowMock.Object, Mock.Of<ITenantProvider>());

        var command = new AddStockCommand(
            ProductId: product.Id,
            Quantity: 0,
            UnitCost: 0m);

        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    // ------------ 4. CreateJournalEntry ------------

    [Fact]
    public async Task CreateJournalEntryHandler_EmptyLines_ThrowsDomainException()
    {
        var handler = new CreateJournalEntryHandler(_journalRepoMock.Object, _uowMock.Object);

        var command = new CreateJournalEntryCommand(
            TenantId: _tenantId,
            EntryDate: DateTime.UtcNow,
            Description: "Test Entry",
            ReferenceNumber: null,
            Lines: new List<JournalLineInput>());

        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task CreateJournalEntryHandler_UnbalancedDebitsCredits_ThrowsDomainException()
    {
        var handler = new CreateJournalEntryHandler(_journalRepoMock.Object, _uowMock.Object);

        var command = new CreateJournalEntryCommand(
            TenantId: _tenantId,
            EntryDate: DateTime.UtcNow,
            Description: "Unbalanced Entry",
            ReferenceNumber: null,
            Lines: new List<JournalLineInput>
            {
                new(Guid.NewGuid(), 100m, 0m, "Debit"),
                new(Guid.NewGuid(), 0m, 50m, "Credit") // Unbalanced
            });

        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<Exception>();
    }

    // ------------ 5. AutoShipOrder ------------

    [Fact]
    public async Task AutoShipOrderHandler_NonExistentOrder_ReturnsFailure()
    {
        _orderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Order?)null);

        var handler = new AutoShipOrderHandler(
            _orderRepoMock.Object, _autoShipmentServiceMock.Object,
            _cargoProviderFactoryMock.Object, _uowMock.Object);

        var command = new AutoShipOrderCommand(
            TenantId: _tenantId,
            OrderId: Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task AutoShipOrderHandler_AlreadyShippedOrder_ReturnsFailure()
    {
        var order = new Order
        {
            TenantId = _tenantId,
            Status = OrderStatus.Confirmed,
            OrderNumber = "ORD-001"
        };
        order.MarkAsShipped("TR12345", CargoProvider.YurticiKargo);
        var orderId = order.Id; // use auto-generated Id
        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var handler = new AutoShipOrderHandler(
            _orderRepoMock.Object, _autoShipmentServiceMock.Object,
            _cargoProviderFactoryMock.Object, _uowMock.Object);

        var command = new AutoShipOrderCommand(
            TenantId: _tenantId,
            OrderId: orderId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already shipped");
    }

    // ------------ 6. SyncPlatform ------------

    [Fact]
    public async Task SyncPlatformHandler_NullCommand_ThrowsArgumentNullException()
    {
        var handler = new SyncPlatformHandler(_orchestratorMock.Object);

        var act = () => handler.Handle(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SyncPlatformHandler_WildcardPlatformCode_CallsSyncAll()
    {
        _orchestratorMock
            .Setup(o => o.SyncAllPlatformsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncResultDto { IsSuccess = true, PlatformCode = "*" });

        var handler = new SyncPlatformHandler(_orchestratorMock.Object);

        var command = new SyncPlatformCommand(
            PlatformCode: "*",
            Direction: SyncDirection.Bidirectional);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _orchestratorMock.Verify(o => o.SyncAllPlatformsAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    // ------------ 7. ImportSettlement ------------

    [Fact]
    public async Task ImportSettlementHandler_EmptyLines_CreatesEmptyBatch()
    {
        var handler = new ImportSettlementHandler(_settlementRepoMock.Object, _uowMock.Object);

        var command = new ImportSettlementCommand(
            TenantId: _tenantId,
            Platform: "Trendyol",
            PeriodStart: DateTime.UtcNow.AddDays(-7),
            PeriodEnd: DateTime.UtcNow,
            TotalGross: 0m,
            TotalCommission: 0m,
            TotalNet: 0m,
            Lines: new List<SettlementLineInput>());

        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().NotThrowAsync();

        _settlementRepoMock.Verify(
            r => r.AddAsync(It.IsAny<SettlementBatch>(), It.IsAny<CancellationToken>()),
            Times.Once());
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task ImportSettlementHandler_ZeroAmounts_Succeeds()
    {
        var handler = new ImportSettlementHandler(_settlementRepoMock.Object, _uowMock.Object);

        var command = new ImportSettlementCommand(
            TenantId: _tenantId,
            Platform: "Trendyol",
            PeriodStart: DateTime.UtcNow.AddDays(-7),
            PeriodEnd: DateTime.UtcNow,
            TotalGross: 0m,
            TotalCommission: 0m,
            TotalNet: 0m,
            Lines: new List<SettlementLineInput>
            {
                new("ORD-001", 0m, 0m, 0m, 0m, 0m, 0m)
            });

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
    }

    // ------------ 8. RunReconciliation ------------

    [Fact]
    public async Task RunReconciliationHandler_EmptyData_ReturnsZeroCounts()
    {
        _scoringServiceMock.Setup(s => s.AutoMatchThreshold).Returns(0.85m);
        _scoringServiceMock.Setup(s => s.ReviewThreshold).Returns(0.70m);

        _settlementRepoMock
            .Setup(r => r.GetUnmatchedAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SettlementBatch>() as IReadOnlyList<SettlementBatch>);

        _bankTxRepoMock
            .Setup(r => r.GetUnreconciledAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BankTransaction>() as IReadOnlyList<BankTransaction>);

        var handler = new RunReconciliationHandler(
            _settlementRepoMock.Object, _bankTxRepoMock.Object,
            _matchRepoMock.Object, _scoringServiceMock.Object, _uowMock.Object);

        var result = await handler.Handle(new RunReconciliationCommand(_tenantId), CancellationToken.None);

        result.AutoMatchedCount.Should().Be(0);
        result.NeedsReviewCount.Should().Be(0);
        result.UnmatchedCount.Should().Be(0);
    }

    [Fact]
    public async Task RunReconciliationHandler_EmptyGuid_TenantId_ReturnsZero()
    {
        _scoringServiceMock.Setup(s => s.AutoMatchThreshold).Returns(0.85m);
        _scoringServiceMock.Setup(s => s.ReviewThreshold).Returns(0.70m);

        _settlementRepoMock
            .Setup(r => r.GetUnmatchedAsync(Guid.Empty, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SettlementBatch>() as IReadOnlyList<SettlementBatch>);

        _bankTxRepoMock
            .Setup(r => r.GetUnreconciledAsync(Guid.Empty, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BankTransaction>() as IReadOnlyList<BankTransaction>);

        var handler = new RunReconciliationHandler(
            _settlementRepoMock.Object, _bankTxRepoMock.Object,
            _matchRepoMock.Object, _scoringServiceMock.Object, _uowMock.Object);

        var result = await handler.Handle(new RunReconciliationCommand(Guid.Empty), CancellationToken.None);

        result.AutoMatchedCount.Should().Be(0);
        result.NeedsReviewCount.Should().Be(0);
    }

    // ------------ 9. RecordCommission ------------

    [Fact]
    public async Task RecordCommissionHandler_ZeroCommission_Succeeds()
    {
        var handler = new RecordCommissionHandler(_commissionRepoMock.Object, _uowMock.Object);

        var command = new RecordCommissionCommand(
            TenantId: _tenantId,
            Platform: "Trendyol",
            GrossAmount: 100m,
            CommissionRate: 0m,
            CommissionAmount: 0m,
            ServiceFee: 0m);

        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().NotThrowAsync();

        _commissionRepoMock.Verify(
            r => r.AddAsync(It.IsAny<CommissionRecord>(), It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task RecordCommissionHandler_NegativeAmounts_DoesNotThrowNRE()
    {
        var handler = new RecordCommissionHandler(_commissionRepoMock.Object, _uowMock.Object);

        var command = new RecordCommissionCommand(
            TenantId: _tenantId,
            Platform: "N11",
            GrossAmount: -50m,
            CommissionRate: -0.10m,
            CommissionAmount: -5m,
            ServiceFee: -1m);

        // Handler delegates to domain -- should not throw NullReferenceException
        var act = () => handler.Handle(command, CancellationToken.None);
        try
        {
            await act();
        }
        catch (Exception ex)
        {
            ex.Should().NotBeOfType<NullReferenceException>();
        }
    }

    // ------------ 10. UpdateProduct ------------

    [Fact]
    public async Task UpdateProductHandler_NullCommand_ThrowsArgumentNullException()
    {
        var handler = new UpdateProductHandler(_productRepoMock.Object, _uowMock.Object);

        var act = () => handler.Handle(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateProductHandler_NonExistentProduct_ReturnsFailure()
    {
        _productRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Product?)null);

        var handler = new UpdateProductHandler(_productRepoMock.Object, _uowMock.Object);

        var command = new UpdateProductCommand(
            ProductId: Guid.NewGuid(),
            Name: "Updated Name");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    // ------------ 11. DeleteProduct ------------

    [Fact]
    public async Task DeleteProductHandler_NonExistentProduct_ReturnsFailure()
    {
        _productRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Product?)null);

        var handler = new DeleteProductHandler(_productRepoMock.Object, _uowMock.Object);

        var command = new DeleteProductCommand(ProductId: Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task DeleteProductHandler_EmptyGuid_ReturnsFailure()
    {
        _productRepoMock.Setup(r => r.GetByIdAsync(Guid.Empty)).ReturnsAsync((Product?)null);

        var handler = new DeleteProductHandler(_productRepoMock.Object, _uowMock.Object);

        var command = new DeleteProductCommand(ProductId: Guid.Empty);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ------------ 12. RemoveStock ------------

    [Fact]
    public async Task RemoveStockHandler_NullCommand_ThrowsArgumentNullException()
    {
        var stockCalc = new StockCalculationService();
        var handler = new RemoveStockHandler(
            _productRepoMock.Object, _movementRepoMock.Object,
            _uowMock.Object, stockCalc, Mock.Of<ITenantProvider>());

        var act = () => handler.Handle(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RemoveStockHandler_NonExistentProduct_ReturnsFailure()
    {
        _productRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Product?)null);

        var stockCalc = new StockCalculationService();
        var handler = new RemoveStockHandler(
            _productRepoMock.Object, _movementRepoMock.Object,
            _uowMock.Object, stockCalc, Mock.Of<ITenantProvider>());

        var command = new RemoveStockCommand(
            ProductId: Guid.NewGuid(),
            Quantity: 5);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    // ------------ 13. CreateQuotation ------------

    [Fact]
    public async Task CreateQuotationHandler_NullLines_Succeeds()
    {
        var handler = new CreateQuotationHandler(_quotationRepoMock.Object, _uowMock.Object);

        var command = new CreateQuotationCommand(
            QuotationNumber: "QT-001",
            ValidUntil: DateTime.UtcNow.AddDays(30),
            CustomerName: "Test Customer",
            Lines: null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.QuotationId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateQuotationHandler_EmptyLines_Succeeds()
    {
        var handler = new CreateQuotationHandler(_quotationRepoMock.Object, _uowMock.Object);

        var command = new CreateQuotationCommand(
            QuotationNumber: "QT-002",
            ValidUntil: DateTime.UtcNow.AddDays(30),
            CustomerName: "Test",
            Lines: new List<CreateQuotationLineInput>());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    // ------------ 14. BulkUpdatePrice (SyncPrice) ------------

    [Fact]
    public async Task BulkUpdatePriceHandler_NullCommand_ThrowsArgumentNullException()
    {
        var handler = new BulkUpdatePriceHandler(_productRepoMock.Object, _uowMock.Object);

        var act = () => handler.Handle(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task BulkUpdatePriceHandler_EmptyItems_ReturnsZeroCounts()
    {
        _productRepoMock
            .Setup(r => r.GetBySKUsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var handler = new BulkUpdatePriceHandler(_productRepoMock.Object, _uowMock.Object);

        var command = new BulkUpdatePriceCommand(
            Items: Array.Empty<BulkUpdatePriceItem>());

        var result = await handler.Handle(command, CancellationToken.None);

        result.SuccessCount.Should().Be(0);
        result.FailedCount.Should().Be(0);
    }

    // ------------ 15. BulkUpdateStock (SyncStock) ------------

    [Fact]
    public async Task BulkUpdateStockHandler_NullCommand_ThrowsArgumentNullException()
    {
        var handler = new BulkUpdateStockHandler(_productRepoMock.Object, _uowMock.Object, new Mock<MesTech.Application.Interfaces.IDistributedLockService>().Object, Microsoft.Extensions.Logging.Abstractions.NullLogger<BulkUpdateStockHandler>.Instance);

        var act = () => handler.Handle(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task BulkUpdateStockHandler_NegativeStock_ReportsFailure()
    {
        _productRepoMock
            .Setup(r => r.GetBySKUsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var lockMock = new Mock<MesTech.Application.Interfaces.IDistributedLockService>();
        lockMock
            .Setup(l => l.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IAsyncDisposable>());

        var handler = new BulkUpdateStockHandler(_productRepoMock.Object, _uowMock.Object, lockMock.Object, Microsoft.Extensions.Logging.Abstractions.NullLogger<BulkUpdateStockHandler>.Instance);

        var command = new BulkUpdateStockCommand(
            Items: new[] { new BulkUpdateStockItem("SKU-001", -10) });

        var result = await handler.Handle(command, CancellationToken.None);

        result.FailedCount.Should().Be(1);
        result.Failures.Should().Contain(f => f.Reason.Contains("negative"));
    }

    // ------------ 16. ImportBankStatement ------------

    [Fact]
    public async Task ImportBankStatementHandler_EmptyTransactions_ReturnsZero()
    {
        var handler = new ImportBankStatementHandler(_bankTxRepoMock.Object, _uowMock.Object);

        var command = new ImportBankStatementCommand(
            TenantId: _tenantId,
            BankAccountId: Guid.NewGuid(),
            Transactions: new List<BankTransactionInput>());

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().Be(0);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task ImportBankStatementHandler_ZeroAmountTransaction_ThrowsArgumentOutOfRange()
    {
        var handler = new ImportBankStatementHandler(_bankTxRepoMock.Object, _uowMock.Object);

        var command = new ImportBankStatementCommand(
            TenantId: _tenantId,
            BankAccountId: Guid.NewGuid(),
            Transactions: new List<BankTransactionInput>
            {
                new(DateTime.UtcNow, 0m, "Zero amount", null, null)
            });

        // Domain entity BankTransaction.Create rejects zero amounts
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    // ------------ 17. CreateCounterparty (CreateCustomer/CreateContact) ------------

    [Fact]
    public async Task CreateCounterpartyHandler_MinimalInput_Succeeds()
    {
        var handler = new CreateCounterpartyHandler(_counterpartyRepoMock.Object, _uowMock.Object);

        var command = new CreateCounterpartyCommand(
            TenantId: _tenantId,
            Name: "Test Customer",
            CounterpartyType: CounterpartyType.Customer);

        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().NotThrowAsync();

        _counterpartyRepoMock.Verify(
            r => r.AddAsync(It.IsAny<Counterparty>(), It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task CreateCounterpartyHandler_EmptyGuidTenant_DoesNotThrowNRE()
    {
        var handler = new CreateCounterpartyHandler(_counterpartyRepoMock.Object, _uowMock.Object);

        var command = new CreateCounterpartyCommand(
            TenantId: Guid.Empty,
            Name: "Test",
            CounterpartyType: CounterpartyType.Supplier);

        var act = () => handler.Handle(command, CancellationToken.None);
        try
        {
            await act();
        }
        catch (Exception ex)
        {
            ex.Should().NotBeOfType<NullReferenceException>();
        }
    }

    // ------------ 18. CreateAccountingExpense ------------

    [Fact]
    public async Task CreateAccountingExpenseHandler_ZeroAmount_ThrowsArgumentOutOfRange()
    {
        var handler = new CreateAccountingExpenseHandler(_expenseRepoMock.Object, _uowMock.Object);

        var command = new CreateAccountingExpenseCommand(
            TenantId: _tenantId,
            Title: "Zero Expense",
            Amount: 0m,
            ExpenseDate: DateTime.UtcNow,
            Source: ExpenseSource.Manual);

        // Domain entity rejects zero/negative amounts with ArgumentOutOfRangeException
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*positive*");
    }

    [Fact]
    public async Task CreateAccountingExpenseHandler_NegativeAmount_DoesNotThrowNRE()
    {
        var handler = new CreateAccountingExpenseHandler(_expenseRepoMock.Object, _uowMock.Object);

        var command = new CreateAccountingExpenseCommand(
            TenantId: _tenantId,
            Title: "Refund",
            Amount: -100m,
            ExpenseDate: DateTime.UtcNow,
            Source: ExpenseSource.Manual);

        var act = () => handler.Handle(command, CancellationToken.None);
        try
        {
            await act();
        }
        catch (Exception ex)
        {
            ex.Should().NotBeOfType<NullReferenceException>();
        }
    }

    // ------------ 19. CreateCariHesap (CreateContact) ------------

    [Fact]
    public async Task CreateCariHesapHandler_MinimalInput_Succeeds()
    {
        var handler = new CreateCariHesapHandler(_cariHesapRepoMock.Object, _uowMock.Object);

        var command = new CreateCariHesapCommand(
            TenantId: _tenantId,
            Name: "Test Cari",
            TaxNumber: null,
            Type: CariHesapType.Musteri,
            Phone: null,
            Email: null,
            Address: null);

        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().NotThrowAsync();

        _cariHesapRepoMock.Verify(r => r.AddAsync(It.IsAny<CariHesap>()), Times.Once());
    }

    [Fact]
    public async Task CreateCariHesapHandler_EmptyName_DoesNotThrowNRE()
    {
        var handler = new CreateCariHesapHandler(_cariHesapRepoMock.Object, _uowMock.Object);

        var command = new CreateCariHesapCommand(
            TenantId: _tenantId,
            Name: "",
            TaxNumber: null,
            Type: CariHesapType.Tedarikci,
            Phone: null,
            Email: null,
            Address: null);

        var act = () => handler.Handle(command, CancellationToken.None);
        try
        {
            await act();
        }
        catch (Exception ex)
        {
            ex.Should().NotBeOfType<NullReferenceException>();
        }
    }

    // ------------ 20. CreateDropshipSupplier (SupplierFeed) ------------

    [Fact]
    public async Task CreateDropshipSupplierHandler_MinimalInput_Succeeds()
    {
        var handler = new CreateDropshipSupplierHandler(_dropshipSupplierRepoMock.Object, _uowMock.Object);

        var command = new CreateDropshipSupplierCommand(
            TenantId: _tenantId,
            Name: "Test Supplier",
            WebsiteUrl: null,
            MarkupType: DropshipMarkupType.Percentage,
            MarkupValue: 15m);

        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().NotThrowAsync();

        _dropshipSupplierRepoMock.Verify(
            r => r.AddAsync(It.IsAny<DropshipSupplier>(), It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task CreateDropshipSupplierHandler_ZeroMarkup_Succeeds()
    {
        var handler = new CreateDropshipSupplierHandler(_dropshipSupplierRepoMock.Object, _uowMock.Object);

        var command = new CreateDropshipSupplierCommand(
            TenantId: _tenantId,
            Name: "Zero Markup Supplier",
            WebsiteUrl: "https://example.com",
            MarkupType: DropshipMarkupType.FixedAmount,
            MarkupValue: 0m);

        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    // ================================================================
    // PART 2: FLUENTVALIDATION AUDIT (10 Tests)
    // ================================================================

    // ------------ V1. CreateJournalEntry Validator -- Empty Description ------------

    [Fact]
    public void CreateJournalEntryValidator_EmptyDescription_Rejects()
    {
        var validator = new CreateJournalEntryValidator();
        var command = new CreateJournalEntryCommand(
            TenantId: Guid.NewGuid(),
            EntryDate: DateTime.UtcNow,
            Description: "",
            ReferenceNumber: null,
            Lines: new List<JournalLineInput>
            {
                new(Guid.NewGuid(), 100m, 0m, null),
                new(Guid.NewGuid(), 0m, 100m, null)
            });

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    // ------------ V2. CreateJournalEntry Validator -- Unbalanced ------------

    [Fact]
    public void CreateJournalEntryValidator_UnbalancedDebitsCredits_Rejects()
    {
        var validator = new CreateJournalEntryValidator();
        var command = new CreateJournalEntryCommand(
            TenantId: Guid.NewGuid(),
            EntryDate: DateTime.UtcNow,
            Description: "Test",
            ReferenceNumber: null,
            Lines: new List<JournalLineInput>
            {
                new(Guid.NewGuid(), 100m, 0m, null),
                new(Guid.NewGuid(), 0m, 50m, null) // Unbalanced
            });

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("debit must equal total credit"));
    }

    // ------------ V3. CreateJournalEntry Validator -- Single Line ------------

    [Fact]
    public void CreateJournalEntryValidator_SingleLine_Rejects()
    {
        var validator = new CreateJournalEntryValidator();
        var command = new CreateJournalEntryCommand(
            TenantId: Guid.NewGuid(),
            EntryDate: DateTime.UtcNow,
            Description: "Test",
            ReferenceNumber: null,
            Lines: new List<JournalLineInput>
            {
                new(Guid.NewGuid(), 100m, 0m, null)
            });

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("at least 2 lines"));
    }

    // ------------ V4. CreateJournalEntry Validator -- Empty TenantId ------------

    [Fact]
    public void CreateJournalEntryValidator_EmptyTenantId_Rejects()
    {
        var validator = new CreateJournalEntryValidator();
        var command = new CreateJournalEntryCommand(
            TenantId: Guid.Empty,
            EntryDate: DateTime.UtcNow,
            Description: "Test",
            ReferenceNumber: null,
            Lines: new List<JournalLineInput>
            {
                new(Guid.NewGuid(), 100m, 0m, null),
                new(Guid.NewGuid(), 0m, 100m, null)
            });

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    // ------------ V5. CreatePlatformCommissionRate Validator -- Negative Rate ------------

    [Fact]
    public void CreatePlatformCommissionRateValidator_NegativeRate_Rejects()
    {
        var validator = new CreatePlatformCommissionRateValidator();
        var command = new CreatePlatformCommissionRateCommand(
            TenantId: Guid.NewGuid(),
            Platform: PlatformType.Trendyol,
            Rate: -5m);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("non-negative"));
    }

    // ------------ V6. CreatePlatformCommissionRate Validator -- Empty TenantId ------------

    [Fact]
    public void CreatePlatformCommissionRateValidator_EmptyTenantId_Rejects()
    {
        var validator = new CreatePlatformCommissionRateValidator();
        var command = new CreatePlatformCommissionRateCommand(
            TenantId: Guid.Empty,
            Platform: PlatformType.Trendyol,
            Rate: 10m);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    // ------------ V7. CreateChartOfAccount Validator -- Empty Code ------------

    [Fact]
    public void CreateChartOfAccountValidator_EmptyCode_Rejects()
    {
        var validator = new CreateChartOfAccountValidator();
        var command = new CreateChartOfAccountCommand(
            TenantId: Guid.NewGuid(),
            Code: "",
            Name: "Test Account",
            AccountType: AccountingAccountType.Asset);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    // ------------ V8. CreateChartOfAccount Validator -- Invalid Code Format ------------

    [Fact]
    public void CreateChartOfAccountValidator_InvalidCodeFormat_Rejects()
    {
        var validator = new CreateChartOfAccountValidator();
        var command = new CreateChartOfAccountCommand(
            TenantId: Guid.NewGuid(),
            Code: "ABC-INVALID",
            Name: "Test Account",
            AccountType: AccountingAccountType.Asset);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("digits and dots"));
    }

    // ------------ V9. UpdateChartOfAccount Validator -- Empty Id ------------

    [Fact]
    public void UpdateChartOfAccountValidator_EmptyId_Rejects()
    {
        var validator = new UpdateChartOfAccountValidator();
        var command = new UpdateChartOfAccountCommand(
            Id: Guid.Empty,
            Name: "Updated Name");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    // ------------ V10. DeleteChartOfAccount Validator -- Empty Id ------------

    [Fact]
    public void DeleteChartOfAccountValidator_EmptyId_Rejects()
    {
        var validator = new DeleteChartOfAccountValidator();
        var command = new DeleteChartOfAccountCommand(
            Id: Guid.Empty);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("required"));
    }
}
