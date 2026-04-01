using FluentAssertions;
using MesTech.Application.Commands.BulkUpdatePrice;
using MesTech.Application.Commands.BulkUpdateStock;
using MesTech.Application.DTOs;
using MesTech.Application.Features.Invoice.Commands;
using MesTech.Application.Features.Invoice.DTOs;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MesTech.Tests.Unit.EdgeCases;

/// <summary>
/// Bulk handler edge cases — empty lists, large batches, mixed valid/invalid SKUs.
/// Dalga 14+15 — DEV 5.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Phase", "Dalga14")]
public class BulkOperationEdgeCaseTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDistributedLockService> _lockService = new();
    private readonly Mock<IInvoiceRepository> _invoiceRepo = new();
    private readonly Mock<IOrderRepository> _orderRepo = new();

    public BulkOperationEdgeCaseTests()
    {
        // Default: lock service always returns a valid disposable handle
        _lockService
            .Setup(l => l.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IAsyncDisposable>());
    }

    #region BulkUpdateStockHandler

    [Fact]
    public async Task BulkUpdateStock_EmptyItemsList_ReturnsSuccessWithZeroCount()
    {
        // Arrange
        _productRepo.Setup(r => r.GetBySKUsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var handler = new BulkUpdateStockHandler(_productRepo.Object, _unitOfWork.Object, _lockService.Object, NullLogger<BulkUpdateStockHandler>.Instance);
        var command = new BulkUpdateStockCommand(Items: Array.Empty<BulkUpdateStockItem>());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.SuccessCount.Should().Be(0);
        result.FailedCount.Should().Be(0);
        result.Failures.Should().BeEmpty();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkUpdateStock_1000Items_AllProcess()
    {
        // Arrange — 1000 items, each with a valid product
        var items = Enumerable.Range(1, 1000)
            .Select(i => new BulkUpdateStockItem($"SKU-{i:D4}", i))
            .ToList();

        var products = items.Select(item => CreateFakeProduct(item.Sku, stock: 0)).ToList();

        // Batch query mock — GetBySKUsAsync returns all products at once
        _productRepo.Setup(r => r.GetBySKUsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Product>)products);

        var handler = new BulkUpdateStockHandler(_productRepo.Object, _unitOfWork.Object, _lockService.Object, NullLogger<BulkUpdateStockHandler>.Instance);
        var command = new BulkUpdateStockCommand(Items: items);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.SuccessCount.Should().Be(1000);
        result.FailedCount.Should().Be(0);
        _productRepo.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.Exactly(1000));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkUpdateStock_NegativeStockValue_ReportsFailure()
    {
        // Arrange
        var items = new List<BulkUpdateStockItem>
        {
            new("SKU-001", -5) // negative stock
        };

        _productRepo.Setup(r => r.GetBySKUsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var handler = new BulkUpdateStockHandler(_productRepo.Object, _unitOfWork.Object, _lockService.Object, NullLogger<BulkUpdateStockHandler>.Instance);
        var command = new BulkUpdateStockCommand(Items: items);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.SuccessCount.Should().Be(0);
        result.FailedCount.Should().Be(1);
        result.Failures.First().Reason.Should().Contain("negative");
    }

    [Fact]
    public async Task BulkUpdateStock_NullRequest_ThrowsArgumentNullException()
    {
        var handler = new BulkUpdateStockHandler(_productRepo.Object, _unitOfWork.Object, _lockService.Object, NullLogger<BulkUpdateStockHandler>.Instance);

        var act = () => handler.Handle(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region BulkUpdatePriceHandler

    [Fact]
    public async Task BulkUpdatePrice_MixedValidInvalidMissingSKUs_ReportsCorrectCounts()
    {
        // Arrange — 3 items: valid, invalid price, missing SKU
        var items = new List<BulkUpdatePriceItem>
        {
            new("SKU-VALID", 99.99m),     // valid
            new("SKU-INVALID", -10m),      // invalid price (<=0)
            new("SKU-MISSING", 50m)        // SKU not found
        };

        var validProduct = CreateFakeProduct("SKU-VALID", stock: 10);
        // Batch query mock — only SKU-VALID is found
        _productRepo.Setup(r => r.GetBySKUsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { validProduct });

        var handler = new BulkUpdatePriceHandler(_productRepo.Object, _unitOfWork.Object);
        var command = new BulkUpdatePriceCommand(Items: items);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.SuccessCount.Should().Be(1);
        result.FailedCount.Should().Be(2);
        result.Failures.Should().Contain(f => f.Sku == "SKU-INVALID" && f.Reason.Contains("greater than 0"));
        result.Failures.Should().Contain(f => f.Sku == "SKU-MISSING" && f.Reason.Contains("not found"));
    }

    [Fact]
    public async Task BulkUpdatePrice_EmptyItemsList_ReturnsZeroSuccess()
    {
        // Arrange
        _productRepo.Setup(r => r.GetBySKUsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var handler = new BulkUpdatePriceHandler(_productRepo.Object, _unitOfWork.Object);
        var command = new BulkUpdatePriceCommand(Items: Array.Empty<BulkUpdatePriceItem>());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.SuccessCount.Should().Be(0);
        result.FailedCount.Should().Be(0);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkUpdatePrice_ZeroPrice_ReportsFailure()
    {
        // Arrange — price == 0 should fail (must be > 0)
        var items = new List<BulkUpdatePriceItem> { new("SKU-001", 0m) };

        _productRepo.Setup(r => r.GetBySKUsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var handler = new BulkUpdatePriceHandler(_productRepo.Object, _unitOfWork.Object);
        var command = new BulkUpdatePriceCommand(Items: items);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.FailedCount.Should().Be(1);
        result.Failures.First().Reason.Should().Contain("greater than 0");
    }

    #endregion

    #region BulkCreateInvoiceHandler

    [Fact]
    public async Task BulkCreateInvoice_EmptyBatch_ReturnsZeroCounts()
    {
        // Arrange
        _orderRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        var logger = NullLogger<BulkCreateInvoiceHandler>.Instance;
        var handler = new BulkCreateInvoiceHandler(_invoiceRepo.Object, _orderRepo.Object, _unitOfWork.Object, logger);
        var command = new BulkCreateInvoiceCommand(
            OrderIds: new List<Guid>(),
            Provider: InvoiceProvider.Sovos);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.TotalRequested.Should().Be(0);
        result.SuccessCount.Should().Be(0);
        result.FailCount.Should().Be(0);
        result.Results.Should().BeEmpty();
    }

    [Fact]
    public async Task BulkCreateInvoice_SingleOrder_ReturnsOneSuccess()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = CreateFakeOrder(orderId);

        _orderRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { order });

        var logger = NullLogger<BulkCreateInvoiceHandler>.Instance;
        var handler = new BulkCreateInvoiceHandler(_invoiceRepo.Object, _orderRepo.Object, _unitOfWork.Object, logger);
        var command = new BulkCreateInvoiceCommand(
            OrderIds: new List<Guid> { orderId },
            Provider: InvoiceProvider.Sovos);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.TotalRequested.Should().Be(1);
        result.SuccessCount.Should().Be(1);
        result.Results.First().Success.Should().BeTrue();
        result.Results.First().InvoiceNumber.Should().StartWith("INV-");
    }

    [Fact]
    public async Task BulkCreateInvoice_NullRequest_ThrowsArgumentNullException()
    {
        var logger = NullLogger<BulkCreateInvoiceHandler>.Instance;
        var handler = new BulkCreateInvoiceHandler(_invoiceRepo.Object, _orderRepo.Object, _unitOfWork.Object, logger);

        var act = () => handler.Handle(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region Helpers

    private static Product CreateFakeProduct(string sku, int stock = 50)
    {
        return new Product
        {
            Name = $"Test Product {sku}",
            SKU = sku,
            Barcode = "1234567890123",
            PurchasePrice = 50m,
            SalePrice = 100m,
            Stock = stock,
            MinimumStock = 5,
            MaximumStock = 1000,
            ReorderLevel = 10,
            CategoryId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };
    }

    private static Order CreateFakeOrder(Guid orderId)
    {
        return new Order
        {
            Id = orderId,
            OrderNumber = $"ORD-{orderId.ToString("N")[..8]}",
            TenantId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test Customer",
            OrderDate = DateTime.UtcNow
        };
    }

    #endregion
}
