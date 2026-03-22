using FluentAssertions;
using MesTech.Application.Features.Reports.InventoryValuationReport;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Tests.Unit._Shared;
using Moq;

namespace MesTech.Tests.Unit.Application.Inventory;

[Trait("Category", "Unit")]
[Trait("Domain", "Inventory")]
public class InventoryValuationReportHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();

    private InventoryValuationReportHandler CreateSut() => new(_productRepo.Object);

    [Fact]
    public async Task Handle_ValidRequest_ReturnsValuationForInStockProducts()
    {
        // Arrange
        var products = new List<Product>
        {
            FakeData.CreateProduct(sku: "VAL-001", stock: 10, purchasePrice: 50m, salePrice: 100m),
            FakeData.CreateProduct(sku: "VAL-002", stock: 5, purchasePrice: 200m, salePrice: 350m),
            FakeData.CreateProduct(sku: "VAL-003", stock: 0, purchasePrice: 80m, salePrice: 120m), // out of stock
        };
        _productRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(products.AsReadOnly());

        var query = new InventoryValuationReportQuery(Guid.NewGuid());
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert — only 2 products with stock > 0
        result.Should().HaveCount(2);

        // Ordered by TotalCostValue descending
        // VAL-002: 5 * 200 = 1000 cost
        // VAL-001: 10 * 50 = 500 cost
        result[0].SKU.Should().Be("VAL-002");
        result[0].TotalCostValue.Should().Be(1000m);
        result[0].TotalSaleValue.Should().Be(1750m);
        result[0].PotentialProfit.Should().Be(750m);

        result[1].SKU.Should().Be("VAL-001");
        result[1].TotalCostValue.Should().Be(500m);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_AllProductsOutOfStock_ReturnsEmptyList()
    {
        // Arrange
        var products = new List<Product>
        {
            FakeData.CreateProduct(sku: "OOS-001", stock: 0),
            FakeData.CreateProduct(sku: "OOS-002", stock: 0),
        };
        _productRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(products.AsReadOnly());

        var query = new InventoryValuationReportQuery(Guid.NewGuid());
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithCategoryFilter_CallsGetByCategoryAsync()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var products = new List<Product>
        {
            FakeData.CreateProduct(sku: "CAT-001", stock: 20, purchasePrice: 30m, salePrice: 60m)
        };
        _productRepo.Setup(r => r.GetByCategoryAsync(categoryId))
            .ReturnsAsync(products.AsReadOnly());

        var query = new InventoryValuationReportQuery(Guid.NewGuid(), CategoryFilter: categoryId);
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].CurrentStock.Should().Be(20);
        _productRepo.Verify(r => r.GetByCategoryAsync(categoryId), Times.Once);
        _productRepo.Verify(r => r.GetAllAsync(), Times.Never);
    }
}
