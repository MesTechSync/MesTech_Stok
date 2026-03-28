using FluentAssertions;
using MesTech.Application.Features.Dashboard.Queries.GetDashboardSummary;
using MesTech.Application.Features.Dashboard.Queries.GetPlatformHealth;
using MesTech.Application.Features.Dashboard.Queries.GetRevenueChart;
using MesTech.Application.Features.Dashboard.Queries.GetSalesToday;
using MesTech.Application.Features.Dashboard.Queries.GetSalesChartData;
using MesTech.Application.Features.Dashboard.Queries.GetPendingInvoices;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using ISyncLogRepository = MesTech.Application.Interfaces.ISyncLogRepository;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Platform;

[Trait("Category", "Unit")]
[Trait("Layer", "Dashboard")]
[Trait("Group", "Handler")]
public class DashboardHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    // ═══ GetDashboardSummary ═══

    [Fact]
    public async Task GetDashboardSummary_NullRequest_Throws()
    {
        var repo = new Mock<IDashboardSummaryRepository>();
        var handler = new GetDashboardSummaryQueryHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetPlatformHealth ═══

    [Fact]
    public async Task GetPlatformHealth_NullRequest_Throws()
    {
        var repo = new Mock<ISyncLogRepository>();
        var handler = new GetPlatformHealthHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetRevenueChart ═══

    [Fact]
    public async Task GetRevenueChart_NullRequest_Throws()
    {
        var repo = new Mock<IOrderRepository>();
        var handler = new GetRevenueChartHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetSalesToday ═══

    [Fact]
    public async Task GetSalesToday_NullRequest_Throws()
    {
        var repo = new Mock<IOrderRepository>();
        var handler = new GetSalesTodayHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetSalesChartData ═══

    [Fact]
    public async Task GetSalesChartData_NullRequest_Throws()
    {
        var repo = new Mock<IOrderRepository>();
        var handler = new GetSalesChartDataHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetPendingInvoices ═══

    [Fact]
    public async Task GetPendingInvoices_NullRequest_Throws()
    {
        var repo = new Mock<IInvoiceRepository>();
        var handler = new GetPendingInvoicesHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }
}
