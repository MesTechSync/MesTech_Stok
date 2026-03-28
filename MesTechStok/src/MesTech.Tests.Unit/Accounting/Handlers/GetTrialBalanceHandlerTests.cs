using FluentAssertions;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Features.Accounting.Queries.GetTrialBalance;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Handlers;

/// <summary>
/// GetTrialBalanceHandler tests — handler-level tests for date validation,
/// grand total computation, and edge cases.
/// Complements the existing Accounting.Queries.GetTrialBalanceHandlerTests.
/// </summary>
[Trait("Category", "Unit")]
public class GetTrialBalanceHandlerTests
{
    private readonly Mock<IChartOfAccountsRepository> _accountRepoMock;
    private readonly Mock<IJournalEntryRepository> _journalRepoMock;
    private readonly GetTrialBalanceHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    private static readonly DateTime PeriodStart = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime PeriodEnd = new(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);

    public GetTrialBalanceHandlerTests()
    {
        _accountRepoMock = new Mock<IChartOfAccountsRepository>();
        _journalRepoMock = new Mock<IJournalEntryRepository>();

        _sut = new GetTrialBalanceHandler(
            _accountRepoMock.Object,
            _journalRepoMock.Object);
    }

    private ChartOfAccounts CreateAccount(string code, string name, AccountType type)
    {
        return ChartOfAccounts.Create(_tenantId, code, name, type);
    }

    private JournalEntry CreatePostedEntry(Guid accountId, decimal debit, decimal credit, DateTime? date = null)
    {
        var entry = JournalEntry.Create(_tenantId, date ?? DateTime.UtcNow, "Test");
        var balancingId = Guid.NewGuid();
        if (debit > 0)
        {
            entry.AddLine(accountId, debit, 0);
            entry.AddLine(balancingId, 0, debit);
        }
        else if (credit > 0)
        {
            entry.AddLine(accountId, 0, credit);
            entry.AddLine(balancingId, credit, 0);
        }
        entry.Post();
        return entry;
    }

    private void SetupRepos(
        IReadOnlyList<ChartOfAccounts> accounts,
        IReadOnlyList<JournalEntry>? openingEntries = null,
        IReadOnlyList<JournalEntry>? periodEntries = null)
    {
        _accountRepoMock
            .Setup(r => r.GetAllAsync(_tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts);

        _journalRepoMock
            .Setup(r => r.GetByDateRangeAsync(
                _tenantId, DateTime.MinValue, PeriodStart.AddDays(-1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(openingEntries ?? new List<JournalEntry>());

        _journalRepoMock
            .Setup(r => r.GetByDateRangeAsync(
                _tenantId, PeriodStart, PeriodEnd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(periodEntries ?? new List<JournalEntry>());
    }

    [Fact]
    public async Task Handle_ValidDateRange_ReturnsTrialBalanceWithCorrectDates()
    {
        // Arrange
        var account = CreateAccount("100", "Kasa", AccountType.Asset);
        var entry = CreatePostedEntry(account.Id, 5000m, 0);

        SetupRepos(
            new List<ChartOfAccounts> { account },
            periodEntries: new List<JournalEntry> { entry });

        var query = new GetTrialBalanceQuery(_tenantId, PeriodStart, PeriodEnd);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.StartDate.Should().Be(PeriodStart);
        result.EndDate.Should().Be(PeriodEnd);
        result.Lines.Should().NotBeEmpty();

        var line = result.Lines.First(l => l.AccountId == account.Id);
        line.PeriodDebit.Should().Be(5000m);
        line.ClosingDebit.Should().Be(5000m);
    }

    [Fact]
    public async Task Handle_NoAccounts_ReturnsEmptyTrialBalance()
    {
        // Arrange
        SetupRepos(new List<ChartOfAccounts>());

        var query = new GetTrialBalanceQuery(_tenantId, PeriodStart, PeriodEnd);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Lines.Should().BeEmpty();
        result.GrandTotalPeriodDebit.Should().Be(0m);
        result.GrandTotalPeriodCredit.Should().Be(0m);
        result.GrandTotalOpeningDebit.Should().Be(0m);
        result.GrandTotalOpeningCredit.Should().Be(0m);
        result.GrandTotalClosingDebit.Should().Be(0m);
        result.GrandTotalClosingCredit.Should().Be(0m);
    }

    [Fact]
    public async Task Handle_OpeningPlusPeriod_ClosingEqualsSum()
    {
        // Arrange
        var account = CreateAccount("100", "Kasa", AccountType.Asset);

        var openingEntry = CreatePostedEntry(account.Id, 2000m, 0);
        var periodEntry = CreatePostedEntry(account.Id, 3000m, 0);

        SetupRepos(
            new List<ChartOfAccounts> { account },
            openingEntries: new List<JournalEntry> { openingEntry },
            periodEntries: new List<JournalEntry> { periodEntry });

        var query = new GetTrialBalanceQuery(_tenantId, PeriodStart, PeriodEnd);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        var line = result.Lines.First(l => l.AccountId == account.Id);
        line.OpeningDebit.Should().Be(2000m);
        line.PeriodDebit.Should().Be(3000m);
        line.ClosingDebit.Should().Be(5000m); // 2000 + 3000
    }

    [Fact]
    public async Task Handle_GrandTotals_AreCorrectSums()
    {
        // Arrange
        var account1 = CreateAccount("100", "Kasa", AccountType.Asset);
        var account2 = CreateAccount("200", "Banka", AccountType.Asset);

        var entry1 = CreatePostedEntry(account1.Id, 1000m, 0);
        var entry2 = CreatePostedEntry(account2.Id, 0, 2000m);

        SetupRepos(
            new List<ChartOfAccounts> { account1, account2 },
            periodEntries: new List<JournalEntry> { entry1, entry2 });

        var query = new GetTrialBalanceQuery(_tenantId, PeriodStart, PeriodEnd);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.GrandTotalPeriodDebit.Should().Be(1000m);
        result.GrandTotalPeriodCredit.Should().Be(2000m);
    }
}
