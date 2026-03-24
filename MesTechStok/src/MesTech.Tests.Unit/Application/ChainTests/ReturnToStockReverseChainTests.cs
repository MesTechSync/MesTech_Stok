using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Domain.Entities;
using MesTech.Domain.Events;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Tests.Unit.Application.ChainTests;

/// <summary>
/// Zincir 5 E2E: ApproveReturn -> ReturnApprovedHandler -> stok artti + ters GL kaydi
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "ChainE2E")]
public class ReturnToStockReverseChainTests
{
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILogger<ReturnApprovedHandler>> _loggerMock = new();

    [Fact]
    public async Task Handle_WhenReturnApproved_ShouldIncreaseProductStock()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var returnId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var product = new Product
        {
            SKU = "RET-001",
            Name = "Iade Urun",
            Stock = 47, // satis sonrasi kalan stok
            MinimumStock = 5,
            CategoryId = Guid.NewGuid(),
            TenantId = tenantId
        };

        var lines = new List<ReturnLineInfoEvent>
        {
            new(productId, "RET-001", 3, 100m)
        };

        _productRepoMock.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new ReturnApprovedHandler(
            _productRepoMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        // Act
        await handler.HandleAsync(returnId, orderId, tenantId, lines, CancellationToken.None);

        // Assert — stok 47'den 50'ye cikti (3 adet geri eklendi)
        product.Stock.Should().Be(50);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenProductNotFound_ShouldSkipAndContinue()
    {
        // Arrange
        var missingProductId = Guid.NewGuid();
        var existingProductId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var existingProduct = new Product
        {
            SKU = "EXISTS-001",
            Name = "Mevcut Urun",
            Stock = 10,
            MinimumStock = 5,
            CategoryId = Guid.NewGuid(),
            TenantId = tenantId
        };

        var lines = new List<ReturnLineInfoEvent>
        {
            new(missingProductId, "MISSING-001", 2, 50m),
            new(existingProductId, "EXISTS-001", 5, 75m)
        };

        _productRepoMock.Setup(r => r.GetByIdAsync(missingProductId)).ReturnsAsync((Product?)null);
        _productRepoMock.Setup(r => r.GetByIdAsync(existingProductId)).ReturnsAsync(existingProduct);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new ReturnApprovedHandler(
            _productRepoMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        // Act — should not throw despite missing product
        await handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), tenantId, lines, CancellationToken.None);

        // Assert — existing product stock increased, missing product skipped
        existingProduct.Stock.Should().Be(15); // 10 + 5
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMultipleLines_ShouldIncreaseAllStocks()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var prod1Id = Guid.NewGuid();
        var prod2Id = Guid.NewGuid();

        var product1 = new Product { SKU = "MR-01", Name = "Multi Return 1", Stock = 20, MinimumStock = 5, CategoryId = Guid.NewGuid(), TenantId = tenantId };
        var product2 = new Product { SKU = "MR-02", Name = "Multi Return 2", Stock = 8, MinimumStock = 5, CategoryId = Guid.NewGuid(), TenantId = tenantId };

        var lines = new List<ReturnLineInfoEvent>
        {
            new(prod1Id, "MR-01", 10, 200m),
            new(prod2Id, "MR-02", 7, 150m)
        };

        _productRepoMock.Setup(r => r.GetByIdAsync(prod1Id)).ReturnsAsync(product1);
        _productRepoMock.Setup(r => r.GetByIdAsync(prod2Id)).ReturnsAsync(product2);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new ReturnApprovedHandler(
            _productRepoMock.Object, _unitOfWorkMock.Object, _loggerMock.Object);

        // Act
        await handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), tenantId, lines, CancellationToken.None);

        // Assert
        product1.Stock.Should().Be(30); // 20 + 10
        product2.Stock.Should().Be(15); // 8 + 7
    }
}
