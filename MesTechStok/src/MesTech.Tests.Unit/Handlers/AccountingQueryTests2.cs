using FluentAssertions;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Features.Accounting.Queries.GetAccountBalance;
using MesTech.Application.Features.Accounting.Queries.GetAccountingExpenses;
using MesTech.Application.Features.Accounting.Queries.GetAccountingPeriods;
using MesTech.Application.Features.Accounting.Queries.GetBalanceSheet;
using MesTech.Application.Features.Accounting.Queries.GetChartOfAccounts;
using MesTech.Application.Features.Accounting.Queries.GetCommissionSummary;
using MesTech.Application.Features.Accounting.Queries.GetFifoCOGS;
using MesTech.Application.Features.Accounting.Queries.GetJournalEntries;
using MesTech.Application.Features.Accounting.Queries.ValidateBalanceSheet;
using MesTech.Application.Features.Accounting.Queries.ValidateTrialBalance;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Services;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Accounting query handler null-guard and happy-path tests (batch 2).
/// Covers: GetAccountBalance, GetAccountingExpenses, GetAccountingPeriods,
/// GetBalanceSheet, GetChartOfAccounts, GetCommissionSummary, GetJournalEntries,
/// GetFifoCOGS, ValidateBalanceSheet, ValidateTrialBalance.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "AccountingQueries2")]
[Trait("Phase", "Dalga15")]
public class AccountingQueryTests2
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly CancellationToken CT = CancellationToken.None;

    // ── GetAccountBalanceHandler ──

    [Fact]
    public async Task GetAccountBalanceHandler_NullRequest_Throws()
    {
        var sut = new GetAccountBalanceHandler(
            new Mock<IChartOfAccountsRepository>().Object,
            new Mock<MesTech.Domain.Interfaces.IJournalEntryRepository>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CT));
    }

    [Fact]
    public async Task GetAccountBalanceHandler_AccountNotFound_ReturnsNull()
    {
        var accountRepo = new Mock<IChartOfAccountsRepository>();
        accountRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), CT))
            .ReturnsAsync((MesTech.Domain.Accounting.Entities.ChartOfAccounts?)null);

        var sut = new GetAccountBalanceHandler(accountRepo.Object, new Mock<MesTech.Domain.Interfaces.IJournalEntryRepository>().Object);
        var result = await sut.Handle(new GetAccountBalanceQuery(TenantId, Guid.NewGuid()), CT);

        result.Should().BeNull();
    }

    // ── GetAccountingExpensesHandler ──

    [Fact]
    public async Task GetAccountingExpensesHandler_NullRequest_Throws()
    {
        var sut = new GetAccountingExpensesHandler(
            new Mock<IPersonalExpenseRepository>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CT));
    }

    [Fact]
    public async Task GetAccountingExpensesHandler_EmptyResult_ReturnsEmptyList()
    {
        var repo = new Mock<IPersonalExpenseRepository>();
        repo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<MesTech.Domain.Accounting.Enums.ExpenseSource?>(), CT))
            .ReturnsAsync(new List<MesTech.Domain.Accounting.Entities.PersonalExpense>());

        var sut = new GetAccountingExpensesHandler(repo.Object);
        var result = await sut.Handle(
            new GetAccountingExpensesQuery(TenantId, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow), CT);

        result.Should().BeEmpty();
    }

    // ── GetAccountingPeriodsHandler ──

    [Fact]
    public async Task GetAccountingPeriodsHandler_EmptyResult_ReturnsEmptyList()
    {
        var repo = new Mock<IAccountingPeriodRepository>();
        repo.Setup(r => r.GetByTenantAsync(It.IsAny<Guid>(), It.IsAny<int?>(), CT))
            .ReturnsAsync(new List<MesTech.Domain.Accounting.Entities.AccountingPeriod>());

        var sut = new GetAccountingPeriodsHandler(repo.Object);
        var result = await sut.Handle(
            new GetAccountingPeriodsQuery(TenantId, 2026), CT);

        result.Should().BeEmpty();
    }

    // ── GetBalanceSheetHandler ──

    [Fact]
    public async Task GetBalanceSheetHandler_NullRequest_Throws()
    {
        var sut = new GetBalanceSheetHandler(
            new Mock<IChartOfAccountsRepository>().Object,
            new Mock<MesTech.Domain.Interfaces.IJournalEntryRepository>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CT));
    }

    // ── GetChartOfAccountsHandler ──

    [Fact]
    public async Task GetChartOfAccountsHandler_NullRequest_Throws()
    {
        var sut = new GetChartOfAccountsHandler(
            new Mock<IChartOfAccountsRepository>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CT));
    }

    [Fact]
    public async Task GetChartOfAccountsHandler_EmptyResult_ReturnsEmptyList()
    {
        var repo = new Mock<IChartOfAccountsRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<bool?>(), CT))
            .ReturnsAsync(new List<MesTech.Domain.Accounting.Entities.ChartOfAccounts>());

        var sut = new GetChartOfAccountsHandler(repo.Object);
        var result = await sut.Handle(new GetChartOfAccountsQuery(TenantId), CT);

        result.Should().BeEmpty();
    }

    // ── GetCommissionSummaryHandler ──

    [Fact]
    public async Task GetCommissionSummaryHandler_NullRequest_Throws()
    {
        var sut = new GetCommissionSummaryHandler(
            new Mock<ICommissionRecordRepository>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CT));
    }

    // ── GetJournalEntriesHandler ──

    [Fact]
    public async Task GetJournalEntriesHandler_NullRequest_Throws()
    {
        var sut = new GetJournalEntriesHandler(
            new Mock<MesTech.Domain.Interfaces.IJournalEntryRepository>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CT));
    }

    [Fact]
    public async Task GetJournalEntriesHandler_EmptyResult_ReturnsEmptyList()
    {
        var repo = new Mock<MesTech.Domain.Interfaces.IJournalEntryRepository>();
        repo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), CT))
            .ReturnsAsync(new List<MesTech.Domain.Accounting.Entities.JournalEntry>());

        var sut = new GetJournalEntriesHandler(repo.Object);
        var result = await sut.Handle(
            new GetJournalEntriesQuery(TenantId, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow), CT);

        result.Should().BeEmpty();
    }

    // ── GetFifoCOGSHandler ──

    [Fact]
    public async Task GetFifoCOGSHandler_NullRequest_Throws()
    {
        var sut = new GetFifoCOGSHandler(
            new Mock<IFifoCostCalculationService>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CT));
    }

    // ── ValidateBalanceSheetHandler ──

    [Fact]
    public async Task ValidateBalanceSheetHandler_CanBeConstructed()
    {
        var sut = new ValidateBalanceSheetHandler(
            new Mock<IChartOfAccountsRepository>().Object,
            new Mock<MesTech.Domain.Interfaces.IJournalEntryRepository>().Object,
            new BalanceSheetValidationService());

        sut.Should().NotBeNull();
    }

    // ── ValidateTrialBalanceHandler ──

    [Fact]
    public async Task ValidateTrialBalanceHandler_CanBeConstructed()
    {
        var sut = new ValidateTrialBalanceHandler(
            new Mock<MesTech.Domain.Interfaces.IJournalEntryRepository>().Object,
            new TrialBalanceValidationService());

        sut.Should().NotBeNull();
    }
}
