using FluentAssertions;
using MesTech.Application.Features.Crm.Queries.GetBitrix24Pipeline;
using MesTech.Application.Interfaces.Crm;
using MesTech.Application.Features.Accounting.Queries.GetCargoComparison;
using MesTech.Application.Features.Erp.Queries.GetErpSyncHistory;
using MesTech.Application.Features.Accounting.Queries.GetFixedExpenseById;
using MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseList;
using MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseSummary;
using MesTech.Application.Queries.GetInventoryPaged;
using MesTech.Application.Queries.GetKarZarar;
using MesTech.Application.Features.System.Kvkk.Queries.GetKvkkAuditLogs;
using MesTech.Application.Features.System.LaunchReadiness;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
[Trait("Phase", "TUR7")]
public class QueryHandlerBatch7Tests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public async Task GetBitrix24Pipeline_NullRequest_Throws()
    {
        var sut = new GetBitrix24PipelineHandler(Mock.Of<IBitrix24Repository>(), Mock.Of<Microsoft.Extensions.Logging.ILogger<GetBitrix24PipelineHandler>>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetCargoComparison_NullRequest_Throws()
    {
        var sut = new GetCargoComparisonHandler(
            Mock.Of<ICargoExpenseRepository>(), Mock.Of<IOrderRepository>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetErpSyncHistory_NullRequest_Throws()
    {
        var sut = new GetErpSyncHistoryHandler(Mock.Of<IErpSyncLogRepository>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetFixedExpenseById_NullRequest_Throws()
    {
        var sut = new GetFixedExpenseByIdHandler(Mock.Of<IFixedExpenseRepository>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetIncomeExpenseList_NullRequest_Throws()
    {
        var sut = new GetIncomeExpenseListHandler(
            Mock.Of<IIncomeRepository>(), Mock.Of<IExpenseRepository>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetIncomeExpenseSummary_NullRequest_Throws()
    {
        var sut = new GetIncomeExpenseSummaryHandler(
            Mock.Of<IIncomeRepository>(), Mock.Of<IExpenseRepository>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetInventoryPaged_NullRequest_Throws()
    {
        var sut = new GetInventoryPagedHandler(Mock.Of<IProductRepository>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetKarZarar_NullRequest_Throws()
    {
        var sut = new GetKarZararHandler(
            Mock.Of<IOrderRepository>(), Mock.Of<IExpenseRepository>(),
            Mock.Of<IIncomeRepository>(), Mock.Of<IProductRepository>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetKvkkAuditLogs_NullRequest_Throws()
    {
        var sut = new GetKvkkAuditLogsHandler(Mock.Of<IKvkkAuditLogRepository>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetLaunchReadiness_ReturnsResult()
    {
        var sut = new GetLaunchReadinessHandler(
            Mock.Of<ITenantRepository>(), Mock.Of<IStoreRepository>(),
            Mock.Of<IProductRepository>(), Mock.Of<IOrderRepository>(),
            Mock.Of<ILogger<GetLaunchReadinessHandler>>());
        var query = new GetLaunchReadinessQuery(_tenantId);
        var result = await sut.Handle(query, CancellationToken.None);
        result.Should().NotBeNull();
    }
}
