using FluentAssertions;
using MesTech.Application.Queries.GetWarehouseStock;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetWarehouseStockHandlerTests
{
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<IWarehouseRepository> _warehouseRepoMock = new();
    private readonly GetWarehouseStockHandler _sut;

    public GetWarehouseStockHandlerTests()
    {
        _sut = new GetWarehouseStockHandler(_productRepoMock.Object, _warehouseRepoMock.Object);
    }

    [Fact]
    public async Task Handle_NonExistentWarehouse_ReturnsEmpty()
    {
        var warehouseId = Guid.NewGuid();
        _warehouseRepoMock.Setup(r => r.GetByIdAsync(warehouseId)).ReturnsAsync((Warehouse?)null);

        var query = new GetWarehouseStockQuery(warehouseId, Guid.NewGuid());
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullProductRepository_Throws()
    {
        var act = () => new GetWarehouseStockHandler(null!, _warehouseRepoMock.Object);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullWarehouseRepository_Throws()
    {
        var act = () => new GetWarehouseStockHandler(_productRepoMock.Object, null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
