using FluentAssertions;
using MesTech.Application.Features.Stock.Queries.GetStockValueReport;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetStockValueReportHandlerTests
{
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly GetStockValueReportHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetStockValueReportHandlerTests()
    {
        _sut = new GetStockValueReportHandler(_productRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WithProducts_ReturnsValuation()
    {
        var pA = new Product { Id = Guid.NewGuid(), Name = "Ürün A", SKU = "SKU-A", PurchasePrice = 50m, SalePrice = 100m };
        pA.SyncStock(10);
        var pB = new Product { Id = Guid.NewGuid(), Name = "Ürün B", SKU = "SKU-B", PurchasePrice = 30m, SalePrice = 60m };
        pB.SyncStock(5);
        var pC = new Product { Id = Guid.NewGuid(), Name = "Stoksuz", SKU = "SKU-C", PurchasePrice = 20m, SalePrice = 40m };
        var products = new List<Product> { pA, pB, pC };
        _productRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(products.AsReadOnly());

        var query = new GetStockValueReportQuery(_tenantId);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.TopValueProducts.Should().HaveCount(2); // Stock=0 excluded
        result.TotalProducts.Should().Be(3);
        result.ZeroStockProducts.Should().Be(1);
        result.TotalValue.Should().Be(10 * 100m + 5 * 60m); // 1300
        result.TotalCostValue.Should().Be(10 * 50m + 5 * 30m); // 650
    }

    [Fact]
    public async Task Handle_EmptyProducts_ReturnsZeros()
    {
        _productRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Product>().AsReadOnly());

        var query = new GetStockValueReportQuery(_tenantId);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.TopValueProducts.Should().BeEmpty();
        result.TotalProducts.Should().Be(0);
        result.TotalValue.Should().Be(0);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
