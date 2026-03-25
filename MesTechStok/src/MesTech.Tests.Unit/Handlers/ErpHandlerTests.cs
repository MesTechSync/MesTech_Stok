using FluentAssertions;
using MesTech.Application.Features.Erp.Queries.GetErpDashboard;
using MesTech.Application.Features.Erp.Queries.GetErpSyncHistory;
using MesTech.Application.Features.Erp.Queries.GetErpSyncLogs;
using MesTech.Application.Features.Reports.ErpReconciliationReport;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Entities.Erp;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class ErpHandlerTests
{
    // ── GetErpDashboardHandler ──

    [Fact]
    public async Task GetErpDashboard_NullRequest_ThrowsException()
    {
        var repo = new Mock<IErpSyncLogRepository>();
        var logger = new Mock<ILogger<GetErpDashboardHandler>>();
        var sut = new GetErpDashboardHandler(repo.Object, logger.Object);

        await Assert.ThrowsAnyAsync<Exception>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetErpDashboard_ValidRequest_ReturnsDashboardDto()
    {
        var repo = new Mock<IErpSyncLogRepository>();
        var logger = new Mock<ILogger<GetErpDashboardHandler>>();
        var tenantId = Guid.NewGuid();

        repo.Setup(r => r.GetPendingRetriesAsync(tenantId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ErpSyncLog>().AsReadOnly());

        var sut = new GetErpDashboardHandler(repo.Object, logger.Object);
        var query = new GetErpDashboardQuery(tenantId);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.PendingRetries.Should().Be(0);
        repo.Verify(r => r.GetPendingRetriesAsync(tenantId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── GetErpSyncHistoryHandler ──

    [Fact]
    public async Task GetErpSyncHistory_NullRequest_ThrowsException()
    {
        var repo = new Mock<IErpSyncLogRepository>();
        var sut = new GetErpSyncHistoryHandler(repo.Object);

        await Assert.ThrowsAnyAsync<Exception>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetErpSyncHistory_ValidRequest_ReturnsPagedLogs()
    {
        var repo = new Mock<IErpSyncLogRepository>();
        var tenantId = Guid.NewGuid();

        repo.Setup(r => r.GetByTenantPagedAsync(tenantId, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ErpSyncLog>().AsReadOnly());

        var sut = new GetErpSyncHistoryHandler(repo.Object);
        var query = new GetErpSyncHistoryQuery(tenantId);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
        repo.Verify(r => r.GetByTenantPagedAsync(tenantId, 1, 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── GetErpSyncLogsHandler ──

    [Fact]
    public async Task GetErpSyncLogs_NullRequest_ThrowsException()
    {
        var repo = new Mock<IErpSyncLogRepository>();
        var sut = new GetErpSyncLogsHandler(repo.Object);

        await Assert.ThrowsAnyAsync<Exception>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetErpSyncLogs_ValidRequest_ReturnsPagedLogs()
    {
        var repo = new Mock<IErpSyncLogRepository>();
        var tenantId = Guid.NewGuid();

        repo.Setup(r => r.GetByTenantPagedAsync(tenantId, 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ErpSyncLog>().AsReadOnly());

        var sut = new GetErpSyncLogsHandler(repo.Object);
        var query = new GetErpSyncLogsQuery(tenantId, 2, 10);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
        repo.Verify(r => r.GetByTenantPagedAsync(tenantId, 2, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── ErpReconciliationReportHandler ──

    [Fact]
    public async Task ErpReconciliationReport_NullRequest_ThrowsException()
    {
        var factory = new Mock<IErpAdapterFactory>();
        var counterpartyRepo = new Mock<ICounterpartyRepository>();
        var logger = new Mock<ILogger<ErpReconciliationReportHandler>>();
        var sut = new ErpReconciliationReportHandler(factory.Object, counterpartyRepo.Object, logger.Object);

        await Assert.ThrowsAnyAsync<Exception>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ErpReconciliationReport_AdapterNotFound_ReturnsPartialReport()
    {
        var factory = new Mock<IErpAdapterFactory>();
        var counterpartyRepo = new Mock<ICounterpartyRepository>();
        var logger = new Mock<ILogger<ErpReconciliationReportHandler>>();
        var tenantId = Guid.NewGuid();

        counterpartyRepo.Setup(r => r.GetAllAsync(tenantId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Counterparty>().AsReadOnly());

        factory.Setup(f => f.GetAdapter(It.IsAny<ErpProvider>()))
            .Throws(new ArgumentException("Not supported"));

        var sut = new ErpReconciliationReportHandler(factory.Object, counterpartyRepo.Object, logger.Object);
        var query = new ErpReconciliationReportQuery(tenantId, ErpProvider.Parasut);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.ErpProvider.Should().Be(ErpProvider.Parasut);
        result.TotalErpContacts.Should().Be(0);
    }
}
