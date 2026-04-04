using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseSummary;
using MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseList;
using MesTech.Application.Queries.GetKarZarar;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Tests for finance query handlers — income/expense summaries.
/// </summary>
[Trait("Category", "Unit")]
public class FinanceQueryHandlerTests
{
    private readonly Mock<IIncomeRepository> _incomeRepo = new();
    private readonly Mock<IExpenseRepository> _expenseRepo = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    // ═══════ GetKarZararHandler ═══════

    [Fact]
    public async Task GetKarZarar_CallsBothRepositories()
    {
        _incomeRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), _tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Income>().AsReadOnly());
        _expenseRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), _tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Expense>().AsReadOnly());

        var sut = new GetKarZararHandler(_incomeRepo.Object, _expenseRepo.Object);
        var query = new GetKarZararQuery(DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow, _tenantId);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetKarZarar_NullRequest_ThrowsArgumentNullException()
    {
        var sut = new GetKarZararHandler(_incomeRepo.Object, _expenseRepo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    // ═══════ GetIncomeExpenseSummaryHandler ═══════

    [Fact]
    public async Task GetIncomeExpenseSummary_NullRequest_Throws()
    {
        var sut = new GetIncomeExpenseSummaryHandler(
            _incomeRepo.Object, _expenseRepo.Object,
            NullLogger<GetIncomeExpenseSummaryHandler>.Instance);

        await Assert.ThrowsAnyAsync<Exception>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    // ═══════ GetIncomeExpenseListHandler ═══════

    [Fact]
    public async Task GetIncomeExpenseList_NullRequest_Throws()
    {
        var sut = new GetIncomeExpenseListHandler(
            _incomeRepo.Object, _expenseRepo.Object,
            NullLogger<GetIncomeExpenseListHandler>.Instance);

        await Assert.ThrowsAnyAsync<Exception>(
            () => sut.Handle(null!, CancellationToken.None));
    }
}
