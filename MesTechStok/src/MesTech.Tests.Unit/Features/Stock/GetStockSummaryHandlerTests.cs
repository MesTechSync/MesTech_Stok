using FluentAssertions;
using MesTech.Application.Features.Stock.Queries.GetStockSummary;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Features.Stock;

[Trait("Category", "Unit")]
public class GetStockSummaryHandlerTests
{
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly GetStockSummaryHandler _sut;

    public GetStockSummaryHandlerTests()
        => _sut = new GetStockSummaryHandler(_productRepoMock.Object);

    [Fact]
    public async Task Handle_ReturnsCorrectSummary()
    {
        var products = new List<Product>
        {
            CreateProduct(stock: 100, minStock: 10, salePrice: 50m),
            CreateProduct(stock: 0, minStock: 5, salePrice: 30m),
            CreateProduct(stock: 3, minStock: 10, salePrice: 20m),
        };
        _productRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        var result = await _sut.Handle(new GetStockSummaryQuery(), CancellationToken.None);

        result.TotalProducts.Should().Be(3);
        result.InStockProducts.Should().Be(2);
        result.OutOfStockProducts.Should().Be(1);
        result.LowStockProducts.Should().Be(1); // stock=3 <= minStock=10
        result.TotalUnits.Should().Be(103);
    }

    [Fact]
    public async Task Handle_EmptyProducts_ReturnsZeros()
    {
        _productRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var result = await _sut.Handle(new GetStockSummaryQuery(), CancellationToken.None);

        result.TotalProducts.Should().Be(0);
        result.TotalStockValue.Should().Be(0);
    }

    private static Product CreateProduct(int stock, int minStock, decimal salePrice)
    {
        var p = Product.Create(Guid.NewGuid(), $"SKU-{Guid.NewGuid():N}", $"Product-{Guid.NewGuid():N}", salePrice);
        typeof(Product).GetProperty("Stock")?.SetValue(p, stock);
        typeof(Product).GetProperty("MinimumStock")?.SetValue(p, minStock);
        return p;
    }
}
