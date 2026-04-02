using FluentAssertions;
using MesTech.Application.Features.Stock.Queries.GetStockValueReport;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

/// <summary>
/// GetStockValueReportHandler: envanter değerleme raporu.
/// TotalCostValue = Sum(Stock × PurchasePrice)
/// TotalSaleValue = Sum(Stock × SalePrice)
/// PotentialProfit = TotalSaleValue - TotalCostValue
/// Stoku 0 olan ürünler dahil edilmez.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Handler")]
[Trait("Group", "ReportChain")]
public class GetStockValueReportHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();

    private GetStockValueReportHandler CreateHandler() =>
        new(_productRepo.Object);

    [Fact]
    public async Task Handle_MultipleProducts_CalculatesCorrectTotals()
    {
        var products = new List<Product>
        {
            new() { Name = "A", SKU = "A-001", Stock = 10, PurchasePrice = 50m, SalePrice = 100m },
            new() { Name = "B", SKU = "B-001", Stock = 20, PurchasePrice = 30m, SalePrice = 60m }
        };
        _productRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(products);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetStockValueReportQuery(Guid.NewGuid()), CancellationToken.None);

        // A: 10×50=500 cost, 10×100=1000 sale
        // B: 20×30=600 cost, 20×60=1200 sale
        result.TotalCostValue.Should().Be(1100m);
        result.TotalSaleValue.Should().Be(2200m);
        result.TotalPotentialProfit.Should().Be(1100m);
        result.TotalProducts.Should().Be(2);
        result.TotalStockUnits.Should().Be(30);
    }

    [Fact]
    public async Task Handle_ZeroStockProducts_ExcludedFromReport()
    {
        var products = new List<Product>
        {
            new() { Name = "A", SKU = "A-001", Stock = 10, PurchasePrice = 50m, SalePrice = 100m },
            new() { Name = "B", SKU = "B-001", Stock = 0, PurchasePrice = 30m, SalePrice = 60m } // stok 0
        };
        _productRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(products);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetStockValueReportQuery(Guid.NewGuid()), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.TotalProducts.Should().Be(1);
    }

    [Fact]
    public async Task Handle_NoProducts_ReturnsEmptyResult()
    {
        _productRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Product>());

        var handler = CreateHandler();
        var result = await handler.Handle(new GetStockValueReportQuery(Guid.NewGuid()), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCostValue.Should().Be(0m);
        result.TotalPotentialProfit.Should().Be(0m);
    }

    [Fact]
    public async Task Handle_ItemsOrderedByCostDescending()
    {
        var products = new List<Product>
        {
            new() { Name = "Cheap", SKU = "C-001", Stock = 5, PurchasePrice = 10m, SalePrice = 20m },
            new() { Name = "Expensive", SKU = "E-001", Stock = 100, PurchasePrice = 200m, SalePrice = 300m }
        };
        _productRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(products);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetStockValueReportQuery(Guid.NewGuid()), CancellationToken.None);

        result.Items[0].SKU.Should().Be("E-001"); // 100×200=20000 cost
        result.Items[1].SKU.Should().Be("C-001"); // 5×10=50 cost
    }
}
