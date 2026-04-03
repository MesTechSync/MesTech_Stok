using FluentAssertions;
using MesTech.Application.Features.Stock.Queries.GetStockPlacements;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Features.Stock;

[Trait("Category", "Unit")]
public class GetStockPlacementsHandlerTests
{
    private readonly Mock<IStockPlacementRepository> _repoMock = new();
    private readonly GetStockPlacementsHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetStockPlacementsHandlerTests()
        => _sut = new GetStockPlacementsHandler(_repoMock.Object);

    [Fact]
    public async Task Handle_ReturnsSortedByWarehouseShelfProduct()
    {
        var placements = new List<MesTech.Domain.Entities.StockPlacement>
        {
            CreatePlacement("Depo-B", "R01-S01", "Ürün A"),
            CreatePlacement("Depo-A", "R01-S02", "Ürün B"),
        };
        _repoMock.Setup(r => r.GetByTenantAsync(_tenantId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(placements);

        var result = await _sut.Handle(new GetStockPlacementsQuery(_tenantId), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].WarehouseName.Should().Be("Depo-A"); // sorted
    }

    [Fact]
    public async Task Handle_EmptyPlacements_ReturnsEmptyList()
    {
        _repoMock.Setup(r => r.GetByTenantAsync(_tenantId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MesTech.Domain.Entities.StockPlacement>());

        var result = await _sut.Handle(new GetStockPlacementsQuery(_tenantId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    private static MesTech.Domain.Entities.StockPlacement CreatePlacement(string warehouse, string shelf, string product)
    {
        var p = new MesTech.Domain.Entities.StockPlacement();
        typeof(MesTech.Domain.Entities.StockPlacement).GetProperty("WarehouseName")?.SetValue(p, warehouse);
        typeof(MesTech.Domain.Entities.StockPlacement).GetProperty("ShelfCode")?.SetValue(p, shelf);
        typeof(MesTech.Domain.Entities.StockPlacement).GetProperty("ProductName")?.SetValue(p, product);
        typeof(MesTech.Domain.Entities.StockPlacement).GetProperty("Quantity")?.SetValue(p, 10);
        return p;
    }
}
