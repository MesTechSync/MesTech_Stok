using FluentAssertions;
using MesTech.Application.Features.Stock.Queries.GetStockLots;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Features.Stock;

[Trait("Category", "Unit")]
public class GetStockLotsHandlerTests
{
    private readonly Mock<IStockLotRepository> _repoMock = new();
    private readonly GetStockLotsHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetStockLotsHandlerTests()
        => _sut = new GetStockLotsHandler(_repoMock.Object);

    [Fact]
    public async Task Handle_ReturnsLotsSortedByReceivedAtDescending()
    {
        var lots = new List<MesTech.Domain.Entities.StockLot>
        {
            CreateLot("LOT-001", DateTime.UtcNow.AddDays(-3), 10, 5, 100m),
            CreateLot("LOT-002", DateTime.UtcNow.AddDays(-1), 20, 15, 200m),
        };
        _repoMock.Setup(r => r.GetByTenantAsync(_tenantId, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lots);

        var result = await _sut.Handle(new GetStockLotsQuery(_tenantId, 50), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].LotNumber.Should().Be("LOT-002"); // newer first
    }

    [Fact]
    public async Task Handle_EmptyRepository_ReturnsEmptyList()
    {
        _repoMock.Setup(r => r.GetByTenantAsync(_tenantId, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MesTech.Domain.Entities.StockLot>());

        var result = await _sut.Handle(new GetStockLotsQuery(_tenantId, 50), CancellationToken.None);

        result.Should().BeEmpty();
    }

    private static MesTech.Domain.Entities.StockLot CreateLot(string lotNumber, DateTime receivedAt, int qty, int remaining, decimal unitCost)
    {
        var lot = MesTech.Domain.Entities.StockLot.Create(Guid.NewGuid(), Guid.NewGuid(), lotNumber, qty, unitCost);
        // Set ReceivedAt via reflection since it's set in Create
        typeof(MesTech.Domain.Entities.StockLot).GetProperty("ReceivedAt")?.SetValue(lot, receivedAt);
        typeof(MesTech.Domain.Entities.StockLot).GetProperty("RemainingQuantity")?.SetValue(lot, remaining);
        return lot;
    }
}
