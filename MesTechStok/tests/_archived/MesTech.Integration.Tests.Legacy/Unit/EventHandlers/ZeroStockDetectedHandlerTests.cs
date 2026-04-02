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
/// RÖNTGEN Zincir 8: ZeroStock → ürünü pasife al.
/// Stok sıfıra düştüğünde ürün deactivate edilir ve platform sync tetiklenir.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "EventHandler")]
[Trait("Group", "Chain8-ZeroStock")]
public class ZeroStockDetectedHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<ZeroStockDetectedEventHandler>> _logger = new();

    public ZeroStockDetectedHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private ZeroStockDetectedEventHandler CreateHandler() =>
        new(_productRepo.Object, _uow.Object, _logger.Object);

    [Fact]
    public async Task HandleAsync_ActiveProduct_Deactivates()
    {
        var product = new Product
        {
            Name = "Test", SKU = "ZERO-001",
            PurchasePrice = 10m, SalePrice = 20m,
            CategoryId = Guid.NewGuid()
        };
        product.IsActive.Should().BeTrue(); // precondition

        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var handler = CreateHandler();
        await handler.HandleAsync(product.Id, "ZERO-001", Guid.NewGuid(), CancellationToken.None);

        product.IsActive.Should().BeFalse();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_AlreadyInactive_SkipsDeactivation()
    {
        var product = new Product
        {
            Name = "Inactive", SKU = "ZERO-002",
            PurchasePrice = 10m, SalePrice = 20m,
            CategoryId = Guid.NewGuid()
        };
        product.Deactivate(); // already inactive
        product.IsActive.Should().BeFalse();

        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var handler = CreateHandler();
        await handler.HandleAsync(product.Id, "ZERO-002", Guid.NewGuid(), CancellationToken.None);

        // Should not call SaveChanges since product was already inactive
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ProductNotFound_DoesNotThrow()
    {
        var missingId = Guid.NewGuid();
        _productRepo.Setup(r => r.GetByIdAsync(missingId)).ReturnsAsync((Product?)null);

        var handler = CreateHandler();
        await handler.HandleAsync(missingId, "MISSING", Guid.NewGuid(), CancellationToken.None);

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
