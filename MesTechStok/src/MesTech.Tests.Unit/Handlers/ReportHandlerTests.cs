using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetProfitReport;
using MesTech.Application.Features.Finance.Queries.GetBudgetSummary;
using MesTech.Application.Features.Finance.Queries.GetProfitLoss;
using MesTech.Application.Features.Reports.CommissionReport;
using MesTech.Application.Features.Reports.CustomerLifetimeValueReport;
using MesTech.Application.Features.Reports.CustomerSegmentReport;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Services;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Tests for report handler queries — Commission, CLV, Segment, ProfitLoss, ProfitReport, BudgetSummary.
/// </summary>
[Trait("Category", "Unit")]
public class ReportHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    // ═══════ CommissionReportHandler ═══════

    [Fact]
    public async Task CommissionReport_EmptyRecords_ReturnsZeroTotals()
    {
        var commissionRepo = new Mock<ICommissionRecordRepository>();
        commissionRepo.Setup(r => r.GetByPlatformAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CommissionRecord>().AsReadOnly());

        var sut = new CommissionReportHandler(commissionRepo.Object);
        var query = new CommissionReportQuery(_tenantId, DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalCommission.Should().Be(0);
        result.TotalOrderCount.Should().Be(0);
        result.PlatformBreakdown.Should().BeEmpty();
    }

    [Fact]
    public async Task CommissionReport_NullRequest_ThrowsAnyException()
    {
        var commissionRepo = new Mock<ICommissionRecordRepository>();
        var sut = new CommissionReportHandler(commissionRepo.Object);

        await Assert.ThrowsAnyAsync<Exception>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    // ═══════ CustomerLifetimeValueReportHandler ═══════

    [Fact]
    public async Task CustomerLifetimeValue_NullRequest_ThrowsArgumentNullException()
    {
        var orderRepo = new Mock<IOrderRepository>();
        var sut = new CustomerLifetimeValueReportHandler(orderRepo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CustomerLifetimeValue_EmptyOrders_ReturnsEmptyList()
    {
        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>().AsReadOnly());

        var sut = new CustomerLifetimeValueReportHandler(orderRepo.Object);
        var query = new CustomerLifetimeValueReportQuery(_tenantId, DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    // ═══════ CustomerSegmentReportHandler ═══════

    [Fact]
    public async Task CustomerSegment_NullRequest_ThrowsArgumentNullException()
    {
        var orderRepo = new Mock<IOrderRepository>();
        var sut = new CustomerSegmentReportHandler(orderRepo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CustomerSegment_EmptyOrders_ReturnsEmptyList()
    {
        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>().AsReadOnly());

        var sut = new CustomerSegmentReportHandler(orderRepo.Object);
        var query = new CustomerSegmentReportQuery(_tenantId, DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    // ═══════ GetProfitLossHandler ═══════

    [Fact]
    public async Task GetProfitLoss_NullRequest_ThrowsArgumentNullException()
    {
        var expenseRepo = new Mock<IFinanceExpenseRepository>();
        var orderRepo = new Mock<IOrderRepository>();
        var sut = new GetProfitLossHandler(expenseRepo.Object, orderRepo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetProfitLoss_EmptyData_ReturnsZeroRevenue()
    {
        var expenseRepo = new Mock<IFinanceExpenseRepository>();
        var orderRepo = new Mock<IOrderRepository>();

        orderRepo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>().AsReadOnly());

        expenseRepo.Setup(r => r.GetTotalByDateRangeAsync(
                It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        expenseRepo.Setup(r => r.GetByTenantAsync(
                It.IsAny<Guid>(), It.IsAny<MesTech.Domain.Enums.ExpenseStatus?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FinanceExpense>().AsReadOnly());

        var sut = new GetProfitLossHandler(expenseRepo.Object, orderRepo.Object);
        var query = new GetProfitLossQuery(_tenantId, 2026, 3);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalRevenue.Should().Be(0);
        result.Year.Should().Be(2026);
        result.Month.Should().Be(3);
    }

    // ═══════ GetProfitReportHandler ═══════

    [Fact]
    public async Task GetProfitReport_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IProfitReportRepository>();
        var profitService = new Mock<IProfitCalculationService>();
        var sut = new GetProfitReportHandler(repo.Object, profitService.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetProfitReport_NoReports_ReturnsNull()
    {
        var repo = new Mock<IProfitReportRepository>();
        repo.Setup(r => r.GetByPeriodAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProfitReport>().AsReadOnly());

        var profitService = new Mock<IProfitCalculationService>();
        var sut = new GetProfitReportHandler(repo.Object, profitService.Object);
        var query = new GetProfitReportQuery(_tenantId, "2026-03");

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().BeNull();
    }

    // ═══════ GetBudgetSummaryHandler ═══════

    [Fact]
    public async Task GetBudgetSummary_NullRequest_ThrowsArgumentNullException()
    {
        var goalRepo = new Mock<IFinancialGoalRepository>();
        var expenseRepo = new Mock<IFinanceExpenseRepository>();
        var sut = new GetBudgetSummaryHandler(goalRepo.Object, expenseRepo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetBudgetSummary_EmptyGoalsAndExpenses_ReturnsZeros()
    {
        var goalRepo = new Mock<IFinancialGoalRepository>();
        var expenseRepo = new Mock<IFinanceExpenseRepository>();

        goalRepo.Setup(r => r.GetActiveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FinancialGoal>().AsReadOnly());

        expenseRepo.Setup(r => r.GetByTenantAsync(
                It.IsAny<Guid>(), It.IsAny<MesTech.Domain.Enums.ExpenseStatus?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FinanceExpense>().AsReadOnly());

        var sut = new GetBudgetSummaryHandler(goalRepo.Object, expenseRepo.Object);
        var query = new GetBudgetSummaryQuery(_tenantId, 2026, 3);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalBudget.Should().Be(0);
        result.TotalSpent.Should().Be(0);
        result.Year.Should().Be(2026);
        result.Month.Should().Be(3);
    }
}
