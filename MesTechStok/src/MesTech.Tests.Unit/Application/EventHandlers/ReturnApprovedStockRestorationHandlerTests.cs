using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Domain.Entities;
using MesTech.Domain.Events;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.EventHandlers;

/// <summary>
/// ReturnApprovedStockRestorationHandler testleri.
/// Zincir 5: İade onay → stok geri ekleme.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class ReturnApprovedStockRestorationDeepTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public ReturnApprovedStockRestorationDeepTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private ReturnApprovedStockRestorationHandler CreateSut() => new(
        _productRepo.Object, _uow.Object, Mock.Of<ILogger<ReturnApprovedStockRestorationHandler>>());

    [Fact]
    public async Task Handle_SingleLine_ShouldRestoreStock()
    {
        var product = new Product
        {
            SKU = "RET-001", Name = "Iade Urun", MinimumStock = 5,
            CategoryId = Guid.NewGuid(), TenantId = TenantId
        };
        product.SyncStock(10, "seed");

        _productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        var lines = new List<ReturnLineInfoEvent>
        {
            new(product.Id, "RET-001", 3, 100m)
        };

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), TenantId, lines, CancellationToken.None);

        product.Stock.Should().Be(13, "10 + 3 iade = 13");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MultipleLines_ShouldRestoreAll()
    {
        var p1 = new Product { SKU = "RET-P1", Name = "U1", MinimumStock = 1, CategoryId = Guid.NewGuid(), TenantId = TenantId };
        p1.SyncStock(20, "seed");
        var p2 = new Product { SKU = "RET-P2", Name = "U2", MinimumStock = 1, CategoryId = Guid.NewGuid(), TenantId = TenantId };
        p2.SyncStock(5, "seed");

        _productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { p1, p2 });

        var lines = new List<ReturnLineInfoEvent>
        {
            new(p1.Id, "RET-P1", 5, 50m),
            new(p2.Id, "RET-P2", 2, 100m)
        };

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), TenantId, lines, CancellationToken.None);

        p1.Stock.Should().Be(25, "20 + 5");
        p2.Stock.Should().Be(7, "5 + 2");
    }

    [Fact]
    public async Task Handle_ProductNotFound_ShouldSkipGracefully()
    {
        _productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var lines = new List<ReturnLineInfoEvent>
        {
            new(Guid.NewGuid(), "MISSING", 3, 100m)
        };

        var sut = CreateSut();
        var act = () => sut.HandleAsync(Guid.NewGuid(), TenantId, lines, CancellationToken.None);

        await act.Should().NotThrowAsync();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyLines_ShouldComplete()
    {
        _productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var sut = CreateSut();

        await sut.HandleAsync(Guid.NewGuid(), TenantId,
            new List<ReturnLineInfoEvent>(), CancellationToken.None);

        _productRepo.Verify(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ZeroStockProduct_ShouldRestoreFromZero()
    {
        var product = new Product
        {
            SKU = "ZERO-RET", Name = "Tukenmis", MinimumStock = 5,
            CategoryId = Guid.NewGuid(), TenantId = TenantId
        };
        product.SyncStock(0, "seed");

        _productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        var lines = new List<ReturnLineInfoEvent> { new(product.Id, "ZERO-RET", 2, 50m) };

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), TenantId, lines, CancellationToken.None);

        product.Stock.Should().Be(2, "0 + 2 iade = 2");
    }
}
