using FluentAssertions;
using MesTech.Application.Features.Platform.Queries.GetOpenCartProducts;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetOpenCartProductsHandlerTests
{
    private readonly Mock<IStoreRepository> _storeRepoMock = new();
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<ILogger<GetOpenCartProductsHandler>> _loggerMock = new();
    private readonly GetOpenCartProductsHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid StoreId = Guid.NewGuid();

    public GetOpenCartProductsHandlerTests()
    {
        _sut = new GetOpenCartProductsHandler(
            _storeRepoMock.Object,
            _productRepoMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidStore_ReturnsProductDtos()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var store = CreateOpenCartStore(productId);
        var product = CreateProduct(productId, "Widget A", "SKU-001", 29.99m, 50, true);

        _storeRepoMock
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);

        _productRepoMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        var query = new GetOpenCartProductsQuery(TenantId, StoreId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Products.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.Products[0].Name.Should().Be("Widget A");
        result.Products[0].SKU.Should().Be("SKU-001");
        result.Products[0].Price.Should().Be(29.99m);
        result.Products[0].Quantity.Should().Be(50);
        result.Products[0].Status.Should().Be("Active");
    }

    [Fact]
    public async Task Handle_StoreNotFound_ReturnsEmptyResult()
    {
        // Arrange
        _storeRepoMock
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Store?)null);

        var query = new GetOpenCartProductsQuery(TenantId, StoreId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Products.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        _productRepoMock.Verify(
            r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_TenantMismatch_ReturnsEmptyResult()
    {
        // Arrange — store exists but belongs to different tenant
        var store = CreateOpenCartStore(Guid.NewGuid());
        store.TenantId = Guid.NewGuid(); // different tenant

        _storeRepoMock
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);

        var query = new GetOpenCartProductsQuery(TenantId, StoreId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Products.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_NonOpenCartStore_ReturnsEmptyResult()
    {
        // Arrange — store exists but is Trendyol, not OpenCart
        var store = CreateOpenCartStore(Guid.NewGuid());
        store.PlatformType = PlatformType.Trendyol;

        _storeRepoMock
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);

        var query = new GetOpenCartProductsQuery(TenantId, StoreId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Products.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_SearchTerm_FiltersProducts()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var store = CreateOpenCartStore(id1, id2);

        var products = new List<Product>
        {
            CreateProduct(id1, "Red Widget", "SKU-RED", 10m, 5, true),
            CreateProduct(id2, "Blue Gadget", "SKU-BLUE", 20m, 8, true)
        };

        _storeRepoMock
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);

        _productRepoMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        var query = new GetOpenCartProductsQuery(TenantId, StoreId, SearchTerm: "Widget");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Products.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.Products[0].Name.Should().Be("Red Widget");
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #region Helpers

    private static Store CreateOpenCartStore(params Guid[] productIds)
    {
        var store = new Store
        {
            TenantId = TenantId,
            PlatformType = PlatformType.OpenCart,
            StoreName = "Test OpenCart Store",
            IsActive = true
        };

        var mappings = productIds.Select(pid => new ProductPlatformMapping
        {
            ProductId = pid,
            StoreId = StoreId,
            PlatformType = PlatformType.OpenCart,
            ExternalProductId = $"OC-{pid.ToString()[..8]}",
            LastSyncDate = DateTime.UtcNow.AddHours(-1)
        }).ToList();

        store.ProductMappings = mappings;
        return store;
    }

    private static Product CreateProduct(Guid id, string name, string sku, decimal price, int stock, bool isActive)
    {
        var product = new Product
        {
            Id = id,
            Name = name,
            SKU = sku,
            SalePrice = price,
            Stock = stock,
            Barcode = $"BAR-{sku}",
            IsActive = isActive
        };

        return product;
    }

    #endregion
}
