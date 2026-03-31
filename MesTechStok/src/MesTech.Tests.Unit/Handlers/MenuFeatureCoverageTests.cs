using FluentAssertions;
using MesTech.Application.Features.Cargo.Queries.GetCargoTrackingList;
using MesTech.Application.Features.Finance.Queries.GetBankAccounts;
using MesTech.Application.Features.Crm.Queries.GetLeads;
using MesTech.Application.Features.Dropshipping.Queries.GetDropshipProfitability;
using MesTech.Application.Features.Reports.ProfitabilityReport;
using MesTech.Application.Features.Reports.SalesAnalytics;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// 80 menü öğesi feature coverage — eksik testleri kapatan batch.
/// CargoTracking, BankAccounts, Leads, DropshipProfit, ProfitabilityReport, SalesAnalytics.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Phase", "MenuCoverage")]
public class MenuFeatureCoverageTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public async Task GetCargoTrackingList_NullRequest_Throws()
    {
        var sut = new GetCargoTrackingListHandler(Mock.Of<IOrderRepository>(), Microsoft.Extensions.Logging.Abstractions.NullLogger<GetCargoTrackingListHandler>.Instance);
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetBankAccounts_NullRequest_Throws()
    {
        var sut = new GetBankAccountsHandler(Mock.Of<IBankAccountRepository>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetLeads_NullRequest_Throws()
    {
        var sut = new GetLeadsHandler(Mock.Of<ICrmLeadRepository>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetDropshipProfitability_NullRequest_Throws()
    {
        var sut = new GetDropshipProfitabilityHandler(
            Mock.Of<IDropshipOrderRepository>(), Mock.Of<IDropshipProductRepository>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ProfitabilityReport_NullRequest_Throws()
    {
        var sut = new ProfitabilityReportHandler(
            Mock.Of<IOrderRepository>(), Mock.Of<IProductRepository>(),
            Mock.Of<ILogger<ProfitabilityReportHandler>>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetSalesAnalytics_NullRequest_Throws()
    {
        var sut = new GetSalesAnalyticsHandler(Mock.Of<IOrderRepository>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
