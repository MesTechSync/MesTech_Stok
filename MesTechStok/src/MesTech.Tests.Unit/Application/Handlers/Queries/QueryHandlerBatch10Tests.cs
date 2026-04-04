using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseList;
using MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseSummary;
using MesTech.Application.Features.System.Kvkk.Queries.GetKvkkAuditLogs;
using MesTech.Application.Features.Reporting.Queries.GetSavedReports;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Reporting;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Queries;

/// <summary>
/// DEV5 Batch 10: Query handler testleri — accounting, compliance, reporting.
/// </summary>

#region GetSavedReports
[Trait("Category", "Unit")]
public class GetSavedReportsHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnReports()
    {
        var repo = new Mock<ISavedReportRepository>();
        repo.Setup(r => r.GetByTenantAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SavedReport>());
        var logger = new Mock<ILogger<GetSavedReportsHandler>>();
        var handler = new GetSavedReportsHandler(repo.Object, logger.Object);
        var result = await handler.Handle(new GetSavedReportsQuery(Guid.NewGuid()), CancellationToken.None);
        result.Should().BeEmpty();
    }
}
#endregion

#region GetKvkkAuditLogs
[Trait("Category", "Unit")]
public class GetKvkkAuditLogsHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnLogs()
    {
        var repo = new Mock<IKvkkAuditLogRepository>();
        repo.Setup(r => r.GetByTenantPagedAsync(It.IsAny<Guid>(), 1, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<KvkkAuditLog>().AsReadOnly() as IReadOnlyList<KvkkAuditLog>, 0));
        var handler = new GetKvkkAuditLogsHandler(repo.Object);
        var result = await handler.Handle(new GetKvkkAuditLogsQuery(Guid.NewGuid()), CancellationToken.None);
        result.Items.Should().BeEmpty();
    }
}
#endregion

#region GetIncomeExpenseList
[Trait("Category", "Unit")]
public class GetIncomeExpenseListHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnCombinedList()
    {
        var incomeRepo = new Mock<IIncomeRepository>();
        var expenseRepo = new Mock<IExpenseRepository>();
        incomeRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid?>()))
            .ReturnsAsync(new List<Income>());
        expenseRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid?>()))
            .ReturnsAsync(new List<Expense>());
        var logger = new Mock<ILogger<GetIncomeExpenseListHandler>>();
        var handler = new GetIncomeExpenseListHandler(incomeRepo.Object, expenseRepo.Object, logger.Object);
        var result = await handler.Handle(
            new GetIncomeExpenseListQuery(Guid.NewGuid()), CancellationToken.None);
        result.Should().NotBeNull();
    }
}
#endregion

#region GetIncomeExpenseSummary
[Trait("Category", "Unit")]
public class GetIncomeExpenseSummaryHandlerTests
{
    [Fact]
    public async Task Handle_NoData_ShouldReturnZeros()
    {
        var incomeRepo = new Mock<IIncomeRepository>();
        var expenseRepo = new Mock<IExpenseRepository>();
        incomeRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid?>()))
            .ReturnsAsync(new List<Income>());
        expenseRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid?>()))
            .ReturnsAsync(new List<Expense>());
        var logger = new Mock<ILogger<GetIncomeExpenseSummaryHandler>>();
        var handler = new GetIncomeExpenseSummaryHandler(incomeRepo.Object, expenseRepo.Object, logger.Object);
        var result = await handler.Handle(
            new GetIncomeExpenseSummaryQuery(Guid.NewGuid(), DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow), CancellationToken.None);
        result.TotalIncome.Should().Be(0);
        result.TotalExpense.Should().Be(0);
    }
}
#endregion
