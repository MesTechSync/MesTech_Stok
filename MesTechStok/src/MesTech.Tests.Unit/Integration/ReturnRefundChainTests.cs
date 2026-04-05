using FluentAssertions;
using MesTech.Application.Commands.ApproveReturn;
using MesTech.Application.EventHandlers;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using IJournalEntryRepository = MesTech.Domain.Interfaces.IJournalEntryRepository;

namespace MesTech.Tests.Unit.Integration;

/// <summary>
/// Return-to-Refund chain integration tests.
/// Verifies the full business chain:
///   ApproveReturn → ReturnApprovedStockRestoration → ReturnJournalReversal
/// Each handler's output is validated and used as input for the next step.
/// </summary>
[Trait("Category", "Unit")]
public class ReturnRefundChainTests
{
    private readonly Mock<IReturnRequestRepository> _returnRepo;
    private readonly Mock<IProductRepository> _productRepo;
    private readonly Mock<IUnitOfWork> _uow;

    private readonly ApproveReturnHandler _approveHandler;
    private readonly ReturnApprovedStockRestorationHandler _stockRestorationHandler;
    private readonly ReturnJournalReversalHandler _journalReversalHandler;

    // Captured for chain verification
    private ReturnRequest? _capturedReturn;

    public ReturnRefundChainTests()
    {
        _returnRepo = new Mock<IReturnRequestRepository>();
        _productRepo = new Mock<IProductRepository>();
        _uow = new Mock<IUnitOfWork>();

        _returnRepo.Setup(r => r.UpdateAsync(It.IsAny<ReturnRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ReturnRequest, CancellationToken>((rr, _) => _capturedReturn = rr)
            .Returns(Task.CompletedTask);

        _approveHandler = new ApproveReturnHandler(
            _returnRepo.Object, _productRepo.Object, _uow.Object);

        _stockRestorationHandler = new ReturnApprovedStockRestorationHandler(
            _productRepo.Object, _uow.Object,
            NullLogger<ReturnApprovedStockRestorationHandler>.Instance);

        _journalReversalHandler = new ReturnJournalReversalHandler(
            _uow.Object, Mock.Of<IJournalEntryRepository>(), NullLogger<ReturnJournalReversalHandler>.Instance);
    }

    private ReturnRequest CreatePendingReturn(
        Guid orderId, Guid tenantId, Product[] products, int[] quantities)
    {
        var rr = ReturnRequest.Create(
            orderId, tenantId, PlatformType.Trendyol,
            ReturnReason.DefectiveProduct, "Test Customer");

        for (int i = 0; i < products.Length; i++)
        {
            var line = new ReturnRequestLine
            {
                TenantId = tenantId,
                ReturnRequestId = rr.Id,
                ProductId = products[i].Id,
                ProductName = products[i].Name,
                SKU = products[i].SKU,
                Quantity = quantities[i],
                UnitPrice = products[i].SalePrice,
                RefundAmount = quantities[i] * products[i].SalePrice
            };
            rr.AddLine(line);
        }

        return rr;
    }

    private readonly List<Product> _registeredProducts = new();

    private Product CreateProduct(string sku, int stock, decimal salePrice)
    {
        var product = new Product
        {
            SKU = sku,
            Name = $"Product {sku}",
            Stock = stock,
            SalePrice = salePrice,
            PurchasePrice = salePrice * 0.6m,
            CategoryId = Guid.NewGuid()
        };
        _registeredProducts.Add(product);
        // Handler uses batch GetByIdsAsync — return all registered products
        _productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<Guid> ids, CancellationToken _) =>
                _registeredProducts.Where(p => ids.Contains(p.Id)).ToList());
        _productRepo.Setup(r => r.UpdateAsync(product, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return product;
    }

    [Fact]
    public async Task FullChain_ApproveReturn_ThenRestoreStock_ThenReversalGL()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var product1 = CreateProduct("RET-01", 10, 200m);
        var product2 = CreateProduct("RET-02", 5, 300m);

        var returnRequest = CreatePendingReturn(
            orderId, tenantId,
            new[] { product1, product2 },
            new[] { 2, 1 });

        _returnRepo.Setup(r => r.GetByIdAsync(returnRequest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(returnRequest);

        // === STEP 1: ApproveReturn ===
        var approveCommand = new ApproveReturnCommand(returnRequest.Id, AutoRestoreStock: true);
        var approveResult = await _approveHandler.Handle(approveCommand, CancellationToken.None);

        approveResult.IsSuccess.Should().BeTrue("Return approval should succeed");
        approveResult.StockRestored.Should().BeTrue("Stock should be auto-restored");
        _capturedReturn.Should().NotBeNull();
        _capturedReturn!.Status.Should().Be(ReturnStatus.Approved);
        _capturedReturn.StockRestored.Should().BeTrue();

        // Stock restoration is now handled by ReturnApprovedStockRestorationHandler (Z5a chain), not ApproveReturnHandler
        product1.Stock.Should().Be(10, "Stock remains unchanged — restoration handled by event handler (Z5a chain)");
        product2.Stock.Should().Be(5, "Stock remains unchanged — restoration handled by event handler (Z5a chain)");

        // === STEP 2: ReturnApprovedStockRestoration (SRP handler — separate event path) ===
        // In the event-driven flow, this handler fires on ReturnApprovedEvent
        // For chain testing, we simulate the event data from the approved return
        var lines = returnRequest.Lines
            .Where(l => l.ProductId.HasValue)
            .Select(l => new ReturnLineInfoEvent(
                l.ProductId!.Value, l.SKU ?? "", l.Quantity, l.UnitPrice))
            .ToList();

        // Reset stock to test the SRP handler independently
        product1.SyncStock(10, "test-reset");
        product2.SyncStock(5, "test-reset");

        await _stockRestorationHandler.HandleAsync(
            returnRequest.Id, tenantId, lines, CancellationToken.None);

        product1.Stock.Should().Be(12, "SRP handler should also restore product1 stock");
        product2.Stock.Should().Be(6, "SRP handler should also restore product2 stock");

        // === STEP 3: ReturnJournalReversal — GL reversal entry ===
        var totalRefund = returnRequest.RefundAmount; // 2*200 + 1*300 = 700
        totalRefund.Should().Be(700m, "Total refund should be 700");

        await _journalReversalHandler.HandleAsync(
            returnRequest.Id, orderId, tenantId, totalRefund, CancellationToken.None);

        // Verify UoW was called for the GL reversal
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeast(3));
    }

    [Fact]
    public async Task Chain_ApproveWithoutAutoRestore_StockNotChanged_RestorationHandlerRestores()
    {
        var tenantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var product = CreateProduct("NO-AUTO", 20, 150m);

        var returnRequest = CreatePendingReturn(
            orderId, tenantId,
            new[] { product },
            new[] { 3 });
        _returnRepo.Setup(r => r.GetByIdAsync(returnRequest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(returnRequest);

        // Step 1: Approve WITHOUT auto-restore
        var result = await _approveHandler.Handle(
            new ApproveReturnCommand(returnRequest.Id, AutoRestoreStock: false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StockRestored.Should().BeFalse("Auto-restore is disabled");
        product.Stock.Should().Be(20, "Stock should remain unchanged when auto-restore is off");

        // Step 2: Event-driven restoration handler does the job
        var lines = new List<ReturnLineInfoEvent>
        {
            new(product.Id, "NO-AUTO", 3, 150m)
        };

        await _stockRestorationHandler.HandleAsync(
            returnRequest.Id, tenantId, lines, CancellationToken.None);

        product.Stock.Should().Be(23, "Restoration handler should add 3 units");

        // Step 3: GL reversal
        await _journalReversalHandler.HandleAsync(
            returnRequest.Id, orderId, tenantId, 450m, CancellationToken.None);

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeast(3));
    }

    [Fact]
    public async Task Chain_ReturnNotFound_FailsEarly_NoDownstreamEffects()
    {
        var missingId = Guid.NewGuid();
        _returnRepo.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>())).ReturnsAsync((ReturnRequest?)null);

        var result = await _approveHandler.Handle(
            new ApproveReturnCommand(missingId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse("Missing return should fail");
        result.ErrorMessage.Should().Contain(missingId.ToString());

        // No UoW call
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never());
        // No product lookups
        _productRepo.Verify(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task Chain_ZeroRefundAmount_SkipsGLReversal()
    {
        var tenantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var returnId = Guid.NewGuid();

        // Zero refund — journal reversal should skip
        await _journalReversalHandler.HandleAsync(
            returnId, orderId, tenantId, 0m, CancellationToken.None);

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never(),
            "Zero refund amount should skip GL reversal entirely");
    }

    [Fact]
    public async Task Chain_ProductMissing_RestorationSkipsGracefully()
    {
        var tenantId = Guid.NewGuid();
        var missingProductId = Guid.NewGuid();

        var existingProduct = CreateProduct("EXISTS", 10, 100m);

        // GetByIdsAsync already set up by CreateProduct — returns only registered products
        // missingProductId won't match any registered product

        var lines = new List<ReturnLineInfoEvent>
        {
            new(missingProductId, "GONE-01", 2, 100m),
            new(existingProduct.Id, "EXISTS", 3, 100m)
        };

        // Should not throw — missing products are logged and skipped
        var act = () => _stockRestorationHandler.HandleAsync(
            Guid.NewGuid(), tenantId, lines, CancellationToken.None);

        await act.Should().NotThrowAsync("Missing products should be skipped gracefully");
        existingProduct.Stock.Should().Be(13, "Existing product stock should still be restored");
    }

    [Fact]
    public async Task Chain_MultiLineReturn_AllLinesRestored_SingleGLEntry()
    {
        var tenantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var products = Enumerable.Range(1, 5).Select(i =>
            CreateProduct($"ML-{i:D2}", i * 10, i * 50m))
            .ToList();

        var lines = products.Select(p =>
            new ReturnLineInfoEvent(p.Id, p.SKU, 2, p.SalePrice))
            .ToList();

        var initialStocks = products.Select(p => p.Stock).ToList();

        // Restoration
        await _stockRestorationHandler.HandleAsync(
            Guid.NewGuid(), tenantId, lines, CancellationToken.None);

        for (int i = 0; i < products.Count; i++)
        {
            products[i].Stock.Should().Be(initialStocks[i] + 2,
                $"Product {products[i].SKU} should have stock increased by 2");
        }

        // GL reversal for total
        var totalRefund = lines.Sum(l => l.Quantity * l.UnitPrice);
        await _journalReversalHandler.HandleAsync(
            Guid.NewGuid(), orderId, tenantId, totalRefund, CancellationToken.None);

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2),
            "One save for stock restoration + one for GL reversal");
    }
}
