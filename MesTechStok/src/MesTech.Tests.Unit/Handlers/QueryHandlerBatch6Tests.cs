using FluentAssertions;
using MesTech.Application.Features.System.Queries.GetBackupHistory;
using MesTech.Application.Features.Finance.Queries.GetBudgetSummary;
using MesTech.Application.Features.Cargo.Queries.GetCargoTrackingList;
using MesTech.Application.Features.Crm.Queries.GetCustomersCrm;
using MesTech.Application.Queries.GetBitrix24DealStatus;
using MesTech.Application.Features.Dropshipping.Queries.GetDropshipProfitability;
using MesTech.Application.Features.Orders.Queries.GetStaleOrders;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Batch handler tests for query handlers — TUR 5 batch 2.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Phase", "TUR5")]
public class QueryHandlerBatch6Tests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    // ═══ GetBackupHistory ═══

    [Fact]
    public async Task GetBackupHistory_ReturnsEmptyList()
    {
        var sut = new GetBackupHistoryHandler(
            Mock.Of<IBackupEntryRepository>(),
            Mock.Of<ILogger<GetBackupHistoryHandler>>());
        var query = new GetBackupHistoryQuery(_tenantId);
        var result = await sut.Handle(query, CancellationToken.None);
        result.Should().BeEmpty();
    }

    // ═══ GetBudgetSummary ═══

    [Fact]
    public async Task GetBudgetSummary_NullRequest_Throws()
    {
        var sut = new GetBudgetSummaryHandler(
            Mock.Of<IFinancialGoalRepository>(), Mock.Of<IFinanceExpenseRepository>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ═══ GetCargoTrackingList ═══

    [Fact]
    public async Task GetCargoTrackingList_NullRequest_Throws()
    {
        var sut = new GetCargoTrackingListHandler(Mock.Of<IOrderRepository>(), Microsoft.Extensions.Logging.Abstractions.NullLogger<GetCargoTrackingListHandler>.Instance);
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ═══ GetCustomersCrm ═══

    [Fact]
    public async Task GetCustomersCrm_NullRequest_Throws()
    {
        var sut = new GetCustomersCrmHandler(Mock.Of<ICrmDashboardQueryService>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ═══ GetBitrix24DealStatus ═══

    [Fact]
    public void GetBitrix24DealStatus_NullRepo_Throws()
    {
        var act = () => new GetBitrix24DealStatusHandler(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // ═══ GetDropshipProfitability ═══

    [Fact]
    public async Task GetDropshipProfitability_NullRequest_Throws()
    {
        var sut = new GetDropshipProfitabilityHandler(
            Mock.Of<IDropshipOrderRepository>(), Mock.Of<IDropshipProductRepository>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ═══ GetStaleOrders ═══

    [Fact]
    public async Task GetStaleOrders_NullRequest_Throws()
    {
        var sut = new GetStaleOrdersQueryHandler(
            Mock.Of<IOrderRepository>(), Mock.Of<ILogger<GetStaleOrdersQueryHandler>>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
