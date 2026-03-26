using FluentAssertions;
using MesTech.Application.Features.Stock.Queries.GetStockTransfers;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetStockTransfersHandlerTests
{
    private readonly Mock<IStockMovementRepository> _movementRepoMock = new();
    private readonly GetStockTransfersHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetStockTransfersHandlerTests()
    {
        _sut = new GetStockTransfersHandler(_movementRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsTransferItems()
    {
        var movements = new List<StockMovement>
        {
            new() { Id = Guid.NewGuid(), ProductName = "Ürün A", ProductSKU = "SKU-A", Quantity = 10, MovementType = "IN", Date = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), ProductName = "Ürün B", ProductSKU = "SKU-B", Quantity = -5, MovementType = "OUT", Date = DateTime.UtcNow }
        };
        _movementRepoMock.Setup(r => r.GetRecentAsync(_tenantId, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(movements.AsReadOnly());

        var query = new GetStockTransfersQuery(_tenantId, 100);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].ProductName.Should().Be("Ürün A");
        result[1].Quantity.Should().Be(-5);
    }

    [Fact]
    public async Task Handle_EmptyMovements_ReturnsEmpty()
    {
        _movementRepoMock.Setup(r => r.GetRecentAsync(_tenantId, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StockMovement>().AsReadOnly());

        var query = new GetStockTransfersQuery(_tenantId);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().BeEmpty();
    }
}
