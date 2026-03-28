using FluentAssertions;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Features.Accounting.Queries.GetTrialBalance;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Queries;

/// <summary>
/// GetTrialBalanceHandler tests — mizan (trial balance) report generation.
/// Verifies opening/period/closing debit-credit summation, filtering, and sorting.
/// </summary>
[Trait("Category", "Unit")]
public class GetTrialBalanceHandlerTests
{
    private readonly Mock<IChartOfAccountsRepository> _accountRepoMock;
    private readonly Mock<IJournalEntryRepository> _journalRepoMock;
    private readonly GetTrialBalanceHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

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

    private JournalEntry CreatePostedEntry(Guid accountId, decimal debit, decimal credit, DateTime? entryDate = null)
    {
        var entry = JournalEntry.Create(_tenantId, entryDate ?? DateTime.UtcNow, "Test entry");

        // Need a balancing line to post, so add two lines
        var otherAccountId = Guid.NewGuid();
        if (debit > 0)
        {
            entry.AddLine(accountId, debit, 0);
            entry.AddLine(otherAccountId, 0, debit);
        }
        else if (credit > 0)
        {
            entry.AddLine(accountId, 0, credit);
            entry.AddLine(otherAccountId, credit, 0);
        }

        entry.Post();
        return entry;
    }

    private JournalEntry CreateUnpostedEntry(Guid accountId, decimal debit, decimal credit)
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Unposted entry");
        var otherAccountId = Guid.NewGuid();
        if (debit > 0)
        {
            entry.AddLine(accountId, debit, 0);
            entry.AddLine(otherAccountId, 0, debit);
        }
        else if (credit > 0)
        {
            entry.AddLine(accountId, 0, credit);
            entry.AddLine(otherAccountId, credit, 0);
        }
        // NOT posted
        return entry;
    }

    private static DateTime PeriodStart => new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static DateTime PeriodEnd => new(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);

    private void SetupRepoForPeriod(IReadOnlyList<ChartOfAccounts> accounts,
        IReadOnlyList<JournalEntry>? openingEntries = null,
        IReadOnlyList<JournalEntry>? periodEntries = null)
    {
        _accountRepoMock.Setup(r => r.GetAllAsync(_tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts);

        // Opening range: (MinValue, StartDate - 1 day)
        _journalRepoMock.Setup(r => r.GetByDateRangeAsync(
                _tenantId,
                DateTime.MinValue,
                PeriodStart.AddDays(-1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(openingEntries ?? new List<JournalEntry>());

        // Period range: (StartDate, EndDate)
        _journalRepoMock.Setup(r => r.GetByDateRangeAsync(
                _tenantId,
                PeriodStart,
                PeriodEnd,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(periodEntries ?? new List<JournalEntry>());
    }

    [Fact]
    public async Task Handle_WithPostedEntries_ReturnsPeriodDebitsCredits()
    {
        // Arrange
        var account = CreateAccount("100", "Kasa", AccountType.Asset);

        var entry1 = CreatePostedEntry(account.Id, 1000m, 0);
        var entry2 = CreatePostedEntry(account.Id, 500m, 0);

        SetupRepoForPeriod(
            new List<ChartOfAccounts> { account },
            periodEntries: new List<JournalEntry> { entry1, entry2 });

        var query = new GetTrialBalanceQuery(_tenantId, PeriodStart, PeriodEnd);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Lines.Should().NotBeEmpty();
        var line = result.Lines.First(l => l.AccountId == account.Id);
        line.PeriodDebit.Should().Be(1500m);
        line.PeriodCredit.Should().Be(0m);
    }

    [Fact]
    public async Task Handle_UnpostedEntries_Excluded()
    {
        // Arrange
        var account = CreateAccount("100", "Kasa", AccountType.Asset);

        var postedEntry = CreatePostedEntry(account.Id, 1000m, 0);
        var unpostedEntry = CreateUnpostedEntry(account.Id, 500m, 0);

        SetupRepoForPeriod(
            new List<ChartOfAccounts> { account },
            periodEntries: new List<JournalEntry> { postedEntry, unpostedEntry });

        var query = new GetTrialBalanceQuery(_tenantId, PeriodStart, PeriodEnd);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert — only posted entry counted
        var line = result.Lines.First(l => l.AccountId == account.Id);
        line.PeriodDebit.Should().Be(1000m);
    }

    [Fact]
    public async Task Handle_DateRange_QueriesCorrectPeriods()
    {
        // Arrange
        var account = CreateAccount("100", "Kasa", AccountType.Asset);

        SetupRepoForPeriod(new List<ChartOfAccounts> { account });

        var query = new GetTrialBalanceQuery(_tenantId, PeriodStart, PeriodEnd);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.StartDate.Should().Be(PeriodStart);
        result.EndDate.Should().Be(PeriodEnd);

        // Verify opening range query
        _journalRepoMock.Verify(r => r.GetByDateRangeAsync(
            _tenantId, DateTime.MinValue, PeriodStart.AddDays(-1), It.IsAny<CancellationToken>()), Times.Once);

        // Verify period range query
        _journalRepoMock.Verify(r => r.GetByDateRangeAsync(
            _tenantId, PeriodStart, PeriodEnd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyJournal_ReturnsEmptyList()
    {
        // Arrange
        var account = CreateAccount("100", "Kasa", AccountType.Asset);

        SetupRepoForPeriod(new List<ChartOfAccounts> { account });

        var query = new GetTrialBalanceQuery(_tenantId, PeriodStart, PeriodEnd);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Lines.Should().BeEmpty();
        result.GrandTotalPeriodDebit.Should().Be(0m);
        result.GrandTotalPeriodCredit.Should().Be(0m);
    }

    [Fact]
    public async Task Handle_MultipleAccounts_SortedByCode()
    {
        // Arrange
        var account300 = CreateAccount("300", "Borc", AccountType.Liability);
        var account100 = CreateAccount("100", "Kasa", AccountType.Asset);
        var account200 = CreateAccount("200", "Banka", AccountType.Asset);

        var entry1 = CreatePostedEntry(account300.Id, 0, 500m);
        var entry2 = CreatePostedEntry(account100.Id, 1000m, 0);
        var entry3 = CreatePostedEntry(account200.Id, 2000m, 0);

        SetupRepoForPeriod(
            new List<ChartOfAccounts> { account300, account100, account200 },
            periodEntries: new List<JournalEntry> { entry1, entry2, entry3 });

        var query = new GetTrialBalanceQuery(_tenantId, PeriodStart, PeriodEnd);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert — lines should be sorted by AccountCode
        var codes = result.Lines.Select(l => l.AccountCode).ToList();
        codes.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Handle_OpeningAndPeriod_CalculatesClosingCorrectly()
    {
        // Arrange
        var account = CreateAccount("100", "Kasa", AccountType.Asset);

        var openingEntry = CreatePostedEntry(account.Id, 3000m, 0);
        var periodDebitEntry = CreatePostedEntry(account.Id, 1500m, 0);
        var periodCreditEntry = CreatePostedEntry(account.Id, 0, 500m);

        SetupRepoForPeriod(
            new List<ChartOfAccounts> { account },
            openingEntries: new List<JournalEntry> { openingEntry },
            periodEntries: new List<JournalEntry> { periodDebitEntry, periodCreditEntry });

        var query = new GetTrialBalanceQuery(_tenantId, PeriodStart, PeriodEnd);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        var line = result.Lines.First(l => l.AccountId == account.Id);
        line.OpeningDebit.Should().Be(3000m);
        line.OpeningCredit.Should().Be(0m);
        line.PeriodDebit.Should().Be(1500m);
        line.PeriodCredit.Should().Be(500m);
        line.ClosingDebit.Should().Be(4500m);   // 3000 + 1500
        line.ClosingCredit.Should().Be(500m);    // 0 + 500
    }

    [Fact]
    public async Task Handle_GrandTotals_SumOfAllLines()
    {
        // Arrange
        var account1 = CreateAccount("100", "Kasa", AccountType.Asset);
        var account2 = CreateAccount("200", "Banka", AccountType.Asset);

        var entry1 = CreatePostedEntry(account1.Id, 1000m, 0);
        var entry2 = CreatePostedEntry(account2.Id, 2000m, 0);

        SetupRepoForPeriod(
            new List<ChartOfAccounts> { account1, account2 },
            periodEntries: new List<JournalEntry> { entry1, entry2 });

        var query = new GetTrialBalanceQuery(_tenantId, PeriodStart, PeriodEnd);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.GrandTotalPeriodDebit.Should().Be(3000m);
    }

    [Fact]
    public async Task Handle_AccountWithZeroActivity_Excluded()
    {
        // Arrange
        var activeAccount = CreateAccount("100", "Kasa", AccountType.Asset);
        var zeroAccount = CreateAccount("200", "Banka", AccountType.Asset);

        // Only activeAccount has entries
        var entry = CreatePostedEntry(activeAccount.Id, 1000m, 0);

        SetupRepoForPeriod(
            new List<ChartOfAccounts> { activeAccount, zeroAccount },
            periodEntries: new List<JournalEntry> { entry });

        var query = new GetTrialBalanceQuery(_tenantId, PeriodStart, PeriodEnd);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert — zeroAccount should NOT appear in lines
        result.Lines.Should().NotContain(l => l.AccountId == zeroAccount.Id);
    }

    [Fact]
    public async Task Handle_SetsAccountTypeInLine()
    {
        // Arrange
        var account = CreateAccount("100", "Kasa", AccountType.Asset);

        var entry = CreatePostedEntry(account.Id, 1000m, 0);

        SetupRepoForPeriod(
            new List<ChartOfAccounts> { account },
            periodEntries: new List<JournalEntry> { entry });

        var query = new GetTrialBalanceQuery(_tenantId, PeriodStart, PeriodEnd);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        var line = result.Lines.First();
        line.AccountType.Should().Be("Asset");
        line.AccountCode.Should().Be("100");
        line.AccountName.Should().Be("Kasa");
    }

    [Fact]
    public async Task Handle_NoAccounts_ReturnsEmptyTrialBalance()
    {
        // Arrange
        SetupRepoForPeriod(new List<ChartOfAccounts>());

        var query = new GetTrialBalanceQuery(_tenantId, PeriodStart, PeriodEnd);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Lines.Should().BeEmpty();
    }
}
