using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class ZeroStockDetectedEventHandlerCommandTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<ZeroStockDetectedEventHandler>> _logger = new();

    private ZeroStockDetectedEventHandler CreateSut() =>
        new(_productRepo.Object, _uow.Object, _logger.Object);

    [Fact]
    public async Task HandleAsync_ProductNotFound_ShouldNotSave()
    {
        _productRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), "SKU-MISS", Guid.NewGuid(), CancellationToken.None);

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ProductNotFound_ShouldNotThrow()
    {
        _productRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var sut = CreateSut();
        var act = async () => await sut.HandleAsync(
            Guid.NewGuid(), "SKU-GONE", Guid.NewGuid(), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task HandleAsync_ActiveProduct_ShouldDeactivateAndSave()
    {
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Name = "Zero Stock Item",
            SKU = "SKU-ZERO",
            TenantId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            PurchasePrice = 50m,
            SalePrice = 100m
        };
        product.IsActive.Should().BeTrue(); // default

        _productRepo.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var sut = CreateSut();
        await sut.HandleAsync(productId, "SKU-ZERO", Guid.NewGuid(), CancellationToken.None);

        product.IsActive.Should().BeFalse();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_AlreadyInactiveProduct_ShouldSkipDeactivation()
    {
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Name = "Already Inactive",
            SKU = "SKU-INACT",
            TenantId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid()
        };
        product.Deactivate(); // make it inactive first

        _productRepo.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var sut = CreateSut();
        await sut.HandleAsync(productId, "SKU-INACT", Guid.NewGuid(), CancellationToken.None);

        // Should NOT save because product was already inactive
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
