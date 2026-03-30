using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.EventHandlers;

/// <summary>
/// RÖNTGEN Zincir 5: ReturnApproved → stok geri yükleme.
/// İade onaylanınca Product.AdjustStock(+qty, CustomerReturn) çağrılır.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "EventHandler")]
[Trait("Group", "Chain5-Return")]
public class ReturnApprovedStockRestorationHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<ReturnApprovedStockRestorationHandler>> _logger = new();

    public ReturnApprovedStockRestorationHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private ReturnApprovedStockRestorationHandler CreateHandler() =>
        new(_productRepo.Object, _uow.Object, _logger.Object);

    private Product MakeProduct(Guid id, int stock)
    {
        var p = new Product { Name = "Test", SKU = "RET-001", PurchasePrice = 10m, SalePrice = 20m, CategoryId = Guid.NewGuid() };
        // Use reflection or direct set since Id is from BaseEntity
        if (stock > 0) p.AdjustStock(stock, StockMovementType.StockIn);
        return p;
    }

    [Fact]
    public async Task HandleAsync_ValidReturn_RestoresStock()
    {
        var product = MakeProduct(Guid.NewGuid(), 10);
        var tenantId = Guid.NewGuid();
        var returnId = Guid.NewGuid();

        _productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        var lines = new List<ReturnLineInfoEvent>
        {
            new(product.Id, "RET-001", 5, 20m)
        };

        var handler = CreateHandler();
        await handler.HandleAsync(returnId, tenantId, lines, CancellationToken.None);

        product.Stock.Should().Be(15); // 10 + 5 returned
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ProductNotFound_SkipsLineAndContinues()
    {
        var missingId = Guid.NewGuid();
        _productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var lines = new List<ReturnLineInfoEvent>
        {
            new(missingId, "MISSING-001", 3, 10m)
        };

        var handler = CreateHandler();
        await handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), lines, CancellationToken.None);

        // Should not throw, just log warning and continue
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_MultipleLines_RestoresAllStocks()
    {
        var p1 = MakeProduct(Guid.NewGuid(), 20);
        var p2 = MakeProduct(Guid.NewGuid(), 30);

        _productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { p1, p2 });

        var lines = new List<ReturnLineInfoEvent>
        {
            new(p1.Id, "SKU-1", 5, 10m),
            new(p2.Id, "SKU-2", 10, 20m)
        };

        var handler = CreateHandler();
        await handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), lines, CancellationToken.None);

        p1.Stock.Should().Be(25);
        p2.Stock.Should().Be(40);
    }
}
