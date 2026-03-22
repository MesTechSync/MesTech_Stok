using FluentAssertions;
using MesTech.Application.Queries.SearchProductsForImageMatch;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Tests.Unit._Shared;
using Moq;

namespace MesTech.Tests.Unit.Application.Products;

[Trait("Category", "Unit")]
[Trait("Domain", "Products")]
public class SearchProductsForImageMatchHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();

    private SearchProductsForImageMatchHandler CreateSut() => new(_productRepo.Object);

    [Fact]
    public async Task Handle_ValidRequest_ReturnsAllProductsWithComputedFields()
    {
        // Arrange
        var products = new List<Product>
        {
            FakeData.CreateProduct(sku: "IMG-001", stock: 50, purchasePrice: 80m, salePrice: 120m, minimumStock: 10),
            FakeData.CreateProduct(sku: "IMG-002", stock: 0, purchasePrice: 50m, salePrice: 100m, minimumStock: 5),
        };
        _productRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(products.AsReadOnly());

        var query = new SearchProductsForImageMatchQuery();
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);

        // First product is normal stock
        var dto1 = result.First(d => d.SKU == "IMG-001");
        dto1.StockStatus.Should().Be("Normal");
        dto1.NeedsReorder.Should().BeFalse();

        // Second product is out of stock
        var dto2 = result.First(d => d.SKU == "IMG-002");
        dto2.StockStatus.Should().Be("OutOfStock");
    }

    [Fact]
    public async Task Handle_NoProducts_ReturnsEmptyList()
    {
        // Arrange
        _productRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Product>().AsReadOnly());

        var query = new SearchProductsForImageMatchQuery();
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_CallsRepositoryExactlyOnce()
    {
        // Arrange
        _productRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Product>().AsReadOnly());

        var sut = CreateSut();

        // Act
        await sut.Handle(new SearchProductsForImageMatchQuery(), CancellationToken.None);

        // Assert
        _productRepo.Verify(r => r.GetAllAsync(), Times.Once);
    }
}
