using FluentAssertions;
using MesTech.Application.Features.Crm.Queries.GetActiveCampaigns;
using MesTech.Application.Features.System.Queries.GetAuditLogs;
using MesTech.Application.Features.Finance.Queries.GetCashFlow;
using MesTech.Application.Features.Finance.Queries.GetCashRegisters;
using MesTech.Application.Queries.GetCategoriesPaged;
using MesTech.Application.Features.Crm.Queries.GetCustomerPoints;
using MesTech.Application.Features.Crm.Queries.GetDeals;
using MesTech.Application.Features.Dropshipping.Queries.GetDropshipDashboard;
using MesTech.Application.Queries.GetBarcodeScanLogs;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Interfaces;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Dropshipping;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Batch handler tests for query handlers — TUR 5 coverage push to 90%.
/// Each handler gets ArgumentNull + empty result tests.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Phase", "TUR5")]
public class QueryHandlerBatch5Tests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    // ═══ GetActiveCampaigns ═══

    [Fact]
    public async Task GetActiveCampaigns_NullRequest_Throws()
    {
        var sut = new GetActiveCampaignsHandler(Mock.Of<ICampaignRepository>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ═══ GetAuditLogs ═══

    [Fact]
    public async Task GetAuditLogs_ReturnsEmptyList()
    {
        var repo = new Mock<IAccessLogRepository>();
        repo.Setup(r => r.GetPagedAsync(
                It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AccessLog>().AsReadOnly());
        var sut = new GetAuditLogsHandler(repo.Object);
        var query = new GetAuditLogsQuery(_tenantId);
        var result = await sut.Handle(query, CancellationToken.None);
        result.Should().BeEmpty();
    }

    // ═══ GetCashFlow ═══

    [Fact]
    public async Task GetCashFlow_NullRequest_Throws()
    {
        var sut = new GetCashFlowHandler(
            Mock.Of<IFinanceExpenseRepository>(), Mock.Of<IOrderRepository>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ═══ GetCashRegisters ═══

    [Fact]
    public async Task GetCashRegisters_NullRequest_Throws()
    {
        var sut = new GetCashRegistersHandler(Mock.Of<ICashRegisterRepository>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ═══ GetCategoriesPaged ═══

    [Fact]
    public async Task GetCategoriesPaged_NullRequest_Throws()
    {
        var repo = new Mock<ICategoryRepository>();
        var sut = new GetCategoriesPagedHandler(repo.Object);
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetCategoriesPaged_EmptyRepo_ReturnsZero()
    {
        var repo = new Mock<ICategoryRepository>();
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Category>().AsReadOnly());
        var sut = new GetCategoriesPagedHandler(repo.Object);

        var query = new GetCategoriesPagedQuery();
        var result = await sut.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(0);
    }

    // ═══ GetCustomerPoints ═══

    [Fact]
    public async Task GetCustomerPoints_NullRequest_Throws()
    {
        var sut = new GetCustomerPointsHandler(Mock.Of<ILoyaltyTransactionRepository>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ═══ GetDeals ═══

    [Fact]
    public async Task GetDeals_NullRequest_Throws()
    {
        var sut = new GetDealsHandler(Mock.Of<ICrmDealRepository>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ═══ GetBarcodeScanLogs ═══

    [Fact]
    public async Task GetBarcodeScanLogs_NullRequest_Throws()
    {
        var repo = new Mock<IBarcodeScanLogRepository>();
        var sut = new GetBarcodeScanLogsHandler(repo.Object);
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
