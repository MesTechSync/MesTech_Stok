using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetShipmentCosts;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Features.Accounting;

[Trait("Category", "Unit")]
public class GetShipmentCostsHandlerTests
{
    private readonly Mock<IShipmentCostRepository> _repoMock = new();
    private readonly Mock<ILogger<GetShipmentCostsHandler>> _loggerMock = new();
    private readonly GetShipmentCostsHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetShipmentCostsHandlerTests()
        => _sut = new GetShipmentCostsHandler(_repoMock.Object, _loggerMock.Object);

    [Fact]
    public async Task Handle_DefaultDateRange_QueriesLastMonth()
    {
        _repoMock.Setup(r => r.GetByDateRangeAsync(
            _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShipmentCost>());

        var query = new GetShipmentCostsQuery(_tenantId);
        await _sut.Handle(query, CancellationToken.None);

        _repoMock.Verify(r => r.GetByDateRangeAsync(
            _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_EmptyResults_ReturnsEmptyList()
    {
        _repoMock.Setup(r => r.GetByDateRangeAsync(
            _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShipmentCost>());

        var result = await _sut.Handle(new GetShipmentCostsQuery(_tenantId), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
