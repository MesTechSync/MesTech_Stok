using FluentAssertions;
using MesTech.Application.Queries.GetInventoryValue;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Domain.Services;
using MesTech.Tests.Unit._Shared;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Queries;

/// <summary>
/// DEV5: GetInventoryValueHandler testi — envanter değer hesaplama.
/// P1: Envanter değeri muhasebe ve raporlama için kritik.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class GetInventoryValueHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly StockCalculationService _stockCalc = new();

    private GetInventoryValueHandler CreateSut() => new(_productRepo.Object, _stockCalc);

    [Fact]
    public async Task Handle_NoProducts_ShouldReturnZeros()
    {
        _productRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var query = new GetInventoryValueQuery();
        var result = await CreateSut().Handle(query, CancellationToken.None);

        result.TotalProducts.Should().Be(0);
        result.TotalStock.Should().Be(0);
        result.LowStockCount.Should().Be(0);
        result.OutOfStockCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithProducts_ShouldCalculateCorrectTotals()
    {
        var products = new List<Product>
        {
            FakeData.CreateProduct(stock: 100, minimumStock: 10),
            FakeData.CreateProduct(stock: 50, minimumStock: 5),
            FakeData.CreateProduct(stock: 3, minimumStock: 10),  // low stock
            FakeData.CreateProduct(stock: 0, minimumStock: 5),   // out of stock
        };

        _productRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        var query = new GetInventoryValueQuery();
        var result = await CreateSut().Handle(query, CancellationToken.None);

        result.TotalProducts.Should().Be(4);
        result.TotalStock.Should().Be(153);
        result.LowStockCount.Should().BeGreaterThan(0);
        result.OutOfStockCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_AllOutOfStock_ShouldReportCorrectly()
    {
        var products = new List<Product>
        {
            FakeData.CreateProduct(stock: 0, minimumStock: 5),
            FakeData.CreateProduct(stock: 0, minimumStock: 10),
        };

        _productRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        var query = new GetInventoryValueQuery();
        var result = await CreateSut().Handle(query, CancellationToken.None);

        result.OutOfStockCount.Should().Be(2);
        result.TotalStock.Should().Be(0);
    }
}
