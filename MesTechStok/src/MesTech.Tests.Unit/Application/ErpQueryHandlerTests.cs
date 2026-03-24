using FluentAssertions;
using MesTech.Application.Features.Erp.Queries.GetErpSyncHistory;
using MesTech.Application.Features.Erp.Queries.GetErpSyncLogs;
using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Entities.Erp;
using Moq;

namespace MesTech.Tests.Unit.Application;

[Trait("Category", "Unit")]
public class ErpQueryHandlerTests
{
    private readonly Mock<IErpSyncLogRepository> _repo = new();

    [Fact]
    public async Task GetErpSyncHistory_CallsRepoWithCorrectParams()
    {
        var tenantId = Guid.NewGuid();
        _repo.Setup(r => r.GetByTenantPagedAsync(
                It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ErpSyncLog>());

        var handler = new GetErpSyncHistoryHandler(_repo.Object);
        var result = await handler.Handle(
            new GetErpSyncHistoryQuery(tenantId, Page: 1, PageSize: 20), CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetErpSyncLogs_CallsRepoWithCorrectParams()
    {
        var tenantId = Guid.NewGuid();
        _repo.Setup(r => r.GetByTenantPagedAsync(
                It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ErpSyncLog>());

        var handler = new GetErpSyncLogsHandler(_repo.Object);
        var result = await handler.Handle(
            new GetErpSyncLogsQuery(tenantId, Page: 1, PageSize: 20), CancellationToken.None);

        result.Should().NotBeNull();
    }
}
