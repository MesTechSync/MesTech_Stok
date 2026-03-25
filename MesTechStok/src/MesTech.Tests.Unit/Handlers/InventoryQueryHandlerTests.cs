using FluentAssertions;
using MesTech.Application.Queries.GetCategories;
using MesTech.Application.Queries.GetInventoryPaged;
using MesTech.Application.Queries.GetInventoryStatistics;
using MesTech.Application.Queries.GetProductById;
using MesTech.Application.Queries.GetProductByBarcode;
using MesTech.Application.Queries.GetLowStockProducts;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Tests for inventory/product query handlers.
/// </summary>
[Trait("Category", "Unit")]
public class InventoryQueryHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<ICategoryRepository> _categoryRepo = new();
    private readonly Mock<IStockMovementRepository> _movementRepo = new();

    // ═══════ GetCategoriesHandler ═══════

    [Fact]
    public async Task GetCategories_CallsRepository()
    {
        _categoryRepo.Setup(r => r.GetActiveAsync())
            .ReturnsAsync(new List<Category>().AsReadOnly());

        var sut = new GetCategoriesHandler(_categoryRepo.Object);
        var result = await sut.Handle(new GetCategoriesQuery(ActiveOnly: true), CancellationToken.None);

        result.Should().NotBeNull();
    }

    // ═══════ GetProductByIdHandler ═══════

    [Fact]
    public async Task GetProductById_Found_ReturnsDto()
    {
        var product = new Product { Name = "Test", SKU = "SKU-1" };
        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var sut = new GetProductByIdHandler(_productRepo.Object);
        var result = await sut.Handle(new GetProductByIdQuery(product.Id), CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetProductById_NotFound_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _productRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Product?)null);

        var sut = new GetProductByIdHandler(_productRepo.Object);
        var result = await sut.Handle(new GetProductByIdQuery(id), CancellationToken.None);

        result.Should().BeNull();
    }

    // ═══════ GetProductByBarcodeHandler ═══════

    [Fact]
    public async Task GetProductByBarcode_Found_ReturnsDto()
    {
        var product = new Product { Name = "Test", SKU = "SKU-BC" };
        _productRepo.Setup(r => r.GetByBarcodeAsync("8680001")).ReturnsAsync(product);

        var sut = new GetProductByBarcodeHandler(_productRepo.Object);
        var result = await sut.Handle(new GetProductByBarcodeQuery("8680001"), CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetProductByBarcode_NotFound_ReturnsNull()
    {
        _productRepo.Setup(r => r.GetByBarcodeAsync("MISSING")).ReturnsAsync((Product?)null);

        var sut = new GetProductByBarcodeHandler(_productRepo.Object);
        var result = await sut.Handle(new GetProductByBarcodeQuery("MISSING"), CancellationToken.None);

        result.Should().BeNull();
    }

    // ═══════ GetLowStockProductsHandler ═══════

    [Fact]
    public async Task GetLowStockProducts_CallsRepository()
    {
        _productRepo.Setup(r => r.GetLowStockAsync())
            .ReturnsAsync(new List<Product>().AsReadOnly());

        var sut = new GetLowStockProductsHandler(_productRepo.Object);
        var result = await sut.Handle(new GetLowStockProductsQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        _productRepo.Verify(r => r.GetLowStockAsync(), Times.Once());
    }

    // ═══════ GetInventoryPagedHandler ═══════

    [Fact]
    public async Task GetInventoryPaged_NullRequest_ThrowsArgumentNullException()
    {
        var sut = new GetInventoryPagedHandler(_productRepo.Object, _categoryRepo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    // ═══════ GetInventoryStatisticsHandler ═══════

    [Fact]
    public async Task GetInventoryStatistics_NullRequest_ThrowsArgumentNullException()
    {
        var sut = new GetInventoryStatisticsHandler(_productRepo.Object, _movementRepo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }
}
