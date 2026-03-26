using FluentAssertions;
using MesTech.Application.Features.Dashboard.Queries.GetStockAlerts;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetStockAlertsHandlerTests
{
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly GetStockAlertsHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetStockAlertsHandlerTests()
    {
        _sut = new GetStockAlertsHandler(_productRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsLowStockAlerts()
    {
        var products = new List<Product>
        {
            new() { Id = Guid.NewGuid(), Name = "Ürün A", SKU = "SKU-A", Stock = 2, MinimumStock = 10 },
            new() { Id = Guid.NewGuid(), Name = "Ürün B", SKU = "SKU-B", Stock = 0, MinimumStock = 5 }
        };
        _productRepoMock.Setup(r => r.GetLowStockAsync()).ReturnsAsync(products.AsReadOnly());

        var query = new GetStockAlertsQuery(_tenantId);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].SKU.Should().Be("SKU-A");
        result[0].CurrentStock.Should().Be(2);
        result[0].MinThreshold.Should().Be(10);
    }

    [Fact]
    public async Task Handle_NoLowStock_ReturnsEmpty()
    {
        _productRepoMock.Setup(r => r.GetLowStockAsync()).ReturnsAsync(new List<Product>().AsReadOnly());

        var query = new GetStockAlertsQuery(_tenantId);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
