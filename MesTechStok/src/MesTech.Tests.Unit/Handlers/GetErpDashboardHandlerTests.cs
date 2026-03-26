using FluentAssertions;
using MesTech.Application.Features.Erp.Queries.GetErpDashboard;
using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Entities.Erp;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetErpDashboardHandlerTests
{
    private readonly Mock<IErpSyncLogRepository> _syncRepoMock = new();
    private readonly GetErpDashboardHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetErpDashboardHandlerTests()
    {
        _sut = new GetErpDashboardHandler(
            _syncRepoMock.Object,
            Mock.Of<ILogger<GetErpDashboardHandler>>());
    }

    [Fact]
    public async Task Handle_ReturnsErpDashboard()
    {
        _syncRepoMock.Setup(r => r.GetPendingRetriesAsync(
            _tenantId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ErpSyncLog>());

        var query = new GetErpDashboardQuery(_tenantId);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.PendingRetries.Should().Be(0);
        result.ConnectedProviders.Should().Be(0);
    }
}
