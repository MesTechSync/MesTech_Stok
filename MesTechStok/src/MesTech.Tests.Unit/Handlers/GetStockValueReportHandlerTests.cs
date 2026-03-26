using FluentAssertions;
using MesTech.Application.Features.Reports.InventoryValuationReport;
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
            new() { Id = Guid.NewGuid(), Name = "Ürün A", SKU = "SKU-A", Stock = 10, PurchasePrice = 50m, SalePrice = 100m },
            new() { Id = Guid.NewGuid(), Name = "Ürün B", SKU = "SKU-B", Stock = 5, PurchasePrice = 30m, SalePrice = 60m },
            new() { Id = Guid.NewGuid(), Name = "Stoksuz", SKU = "SKU-C", Stock = 0, PurchasePrice = 20m, SalePrice = 40m }
        };
        _productRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(products.AsReadOnly());

        var query = new GetStockValueReportQuery(_tenantId);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Items.Should().HaveCount(2); // Stock=0 excluded
    }

    [Fact]
    public async Task Handle_WithCategoryFilter_QueriesByCategory()
    {
        var categoryId = Guid.NewGuid();
        var products = new List<Product>
        {
            new() { Id = Guid.NewGuid(), Name = "Filtered", SKU = "F-1", Stock = 3, PurchasePrice = 10m, SalePrice = 20m }
        };
        _productRepoMock.Setup(r => r.GetByCategoryAsync(categoryId)).ReturnsAsync(products.AsReadOnly());

        var query = new GetStockValueReportQuery(_tenantId, CategoryFilter: categoryId);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        _productRepoMock.Verify(r => r.GetByCategoryAsync(categoryId), Times.Once);
        _productRepoMock.Verify(r => r.GetAllAsync(), Times.Never);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
