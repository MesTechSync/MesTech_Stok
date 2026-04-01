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
        var products = new List<Product>
        {
            new Product { Id = Guid.NewGuid(), Name = "Ürün A", SKU = "SKU-A", Stock = 10, PurchasePrice = 50m, SalePrice = 100m },
            new Product { Id = Guid.NewGuid(), Name = "Ürün B", SKU = "SKU-B", Stock = 5, PurchasePrice = 30m, SalePrice = 60m },
            new Product { Id = Guid.NewGuid(), Name = "Stoksuz", SKU = "SKU-C", Stock = 0, PurchasePrice = 20m, SalePrice = 40m }
        };
        _productRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(products.AsReadOnly());

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
        _productRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Product>().AsReadOnly());

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
