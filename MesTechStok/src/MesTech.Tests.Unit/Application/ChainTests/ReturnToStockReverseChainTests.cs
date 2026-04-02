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
/// Zincir 5 E2E: ApproveReturn -> ReturnApprovedStockRestorationHandler -> stok artti
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "ChainE2E")]
public class ReturnToStockReverseChainTests
{
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILogger<ReturnApprovedStockRestorationHandler>> _loggerMock = new();

    [Fact]
    public async Task Handle_WhenReturnApproved_ShouldIncreaseProductStock()
    {
        var tenantId = Guid.NewGuid();
        var returnId = Guid.NewGuid();

        var product = new Product
        {
            SKU = "RET-001", Name = "Iade Urun", Stock = 47,
            MinimumStock = 5, CategoryId = Guid.NewGuid(), TenantId = tenantId
        };

        // Use product.Id so productMap.TryGetValue matches
        var lines = new List<ReturnLineInfoEvent> { new(product.Id, "RET-001", 3, 100m) };

        _productRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new ReturnApprovedStockRestorationHandler(
            _productRepoMock.Object, _unitOfWorkMock.Object, _loggerMock.Object);

        await handler.HandleAsync(returnId, tenantId, lines, CancellationToken.None);

        product.Stock.Should().Be(50);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenProductNotFound_ShouldSkipAndContinue()
    {
        var existingProduct = new Product
        {
            SKU = "EXISTS-001", Name = "Mevcut Urun", Stock = 10,
            MinimumStock = 5, CategoryId = Guid.NewGuid(), TenantId = Guid.NewGuid()
        };

        // Use existingProduct.Id for the matching line, random Guid for missing
        var lines = new List<ReturnLineInfoEvent>
        {
            new(Guid.NewGuid(), "MISSING-001", 2, 50m),
            new(existingProduct.Id, "EXISTS-001", 5, 75m)
        };

        _productRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { existingProduct });
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new ReturnApprovedStockRestorationHandler(
            _productRepoMock.Object, _unitOfWorkMock.Object, _loggerMock.Object);

        await handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), lines, CancellationToken.None);

        existingProduct.Stock.Should().Be(15);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
