using FluentAssertions;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Features.Accounting.Queries.GetBalanceSheet;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Queries;

/// <summary>
/// GetBalanceSheetHandler tests — bilanco (balance sheet) report generation.
/// Verifies accounting equation: Assets = Liabilities + Equity.
/// BalanceSheetDto now returns sections with line items.
/// </summary>
[Trait("Category", "Unit")]
public class GetBalanceSheetHandlerTests
{
    private readonly Mock<IChartOfAccountsRepository> _accountRepoMock;
    private readonly Mock<IJournalEntryRepository> _journalRepoMock;
    private readonly GetBalanceSheetHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetBalanceSheetHandlerTests()
    {
        _accountRepoMock = new Mock<IChartOfAccountsRepository>();
        _journalRepoMock = new Mock<IJournalEntryRepository>();

        _sut = new GetBalanceSheetHandler(
            _accountRepoMock.Object,
            _journalRepoMock.Object);
    }

    private ChartOfAccounts CreateAccount(string code, string name, AccountType type)
    {
        return ChartOfAccounts.Create(_tenantId, code, name, type);
    }

    private JournalEntry CreatePostedEntry(List<(Guid accountId, decimal debit, decimal credit)> lines, DateTime? entryDate = null)
    {
        var entry = JournalEntry.Create(_tenantId, entryDate ?? DateTime.UtcNow, "Balance sheet test");

        foreach (var (accountId, debit, credit) in lines)
        {
            entry.AddLine(accountId, debit, credit);
        }

        entry.Post();
        return entry;
    }

    [Fact]
    public async Task Handle_AssetsEqualLiabilitiesPlusEquity_IsBalancedTrue()
    {
        // Arrange — Assets: 10000 debit, Liabilities: 4000 credit, Equity: 6000 credit
        var assetAccount = CreateAccount("100", "Kasa", AccountType.Asset);
        var liabilityAccount = CreateAccount("300", "Borc", AccountType.Liability);
        var equityAccount = CreateAccount("500", "Sermaye", AccountType.Equity);

        _accountRepoMock.Setup(r => r.GetAllAsync(_tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChartOfAccounts> { assetAccount, liabilityAccount, equityAccount });

        var entry = CreatePostedEntry(new List<(Guid, decimal, decimal)>
        {
            (assetAccount.Id, 10000m, 0m),
            (liabilityAccount.Id, 0m, 4000m),
            (equityAccount.Id, 0m, 6000m)
        });

        _journalRepoMock.Setup(r => r.GetByDateRangeAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry> { entry });

        var query = new GetBalanceSheetQuery(_tenantId, DateTime.UtcNow);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Assets.Total.Should().Be(10000m);
        result.Liabilities.Total.Should().Be(4000m);
        result.Equity.Total.Should().Be(6000m);
        result.IsBalanced.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Imbalanced_IsBalancedFalse()
    {
        // Arrange — Create an imbalanced scenario via separate entries
        var assetAccount = CreateAccount("100", "Kasa", AccountType.Asset);
        var equityAccount = CreateAccount("500", "Sermaye", AccountType.Equity);

        _accountRepoMock.Setup(r => r.GetAllAsync(_tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChartOfAccounts> { assetAccount, equityAccount });

        // Asset: debit 10000, Equity: credit 10000 — balanced initially
        var entry1 = CreatePostedEntry(new List<(Guid, decimal, decimal)>
        {
            (assetAccount.Id, 10000m, 0m),
            (equityAccount.Id, 0m, 10000m)
        });

        // This additional entry only debits asset (with balancing to another account not in our list)
        var otherAccountId = Guid.NewGuid();
        var entry2 = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Extra asset");
        entry2.AddLine(assetAccount.Id, 5000m, 0m);
        entry2.AddLine(otherAccountId, 0m, 5000m);
        entry2.Post();

        _journalRepoMock.Setup(r => r.GetByDateRangeAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry> { entry1, entry2 });

        var query = new GetBalanceSheetQuery(_tenantId, DateTime.UtcNow);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert — Assets 15000, Equity 10000, missing account makes it imbalanced
        result.IsBalanced.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_IncludesNetIncome_InEquitySection()
    {
        // Arrange — Revenue and Expense affect equity through net income
        var assetAccount = CreateAccount("100", "Kasa", AccountType.Asset);
        var equityAccount = CreateAccount("500", "Sermaye", AccountType.Equity);
        var revenueAccount = CreateAccount("600", "Satis Geliri", AccountType.Revenue);
        var expenseAccount = CreateAccount("770", "Genel Gider", AccountType.Expense);

        _accountRepoMock.Setup(r => r.GetAllAsync(_tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChartOfAccounts> { assetAccount, equityAccount, revenueAccount, expenseAccount });

        // Assets = 10000 debit, Equity = 5000 credit, Revenue = 8000 credit, Expense = 3000 debit
        var entry = CreatePostedEntry(new List<(Guid, decimal, decimal)>
        {
            (assetAccount.Id, 10000m, 0m),
            (equityAccount.Id, 0m, 5000m),
            (revenueAccount.Id, 0m, 8000m),
            (expenseAccount.Id, 3000m, 0m)
        });

        _journalRepoMock.Setup(r => r.GetByDateRangeAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry> { entry });

        var query = new GetBalanceSheetQuery(_tenantId, DateTime.UtcNow);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert — Equity section includes net income line
        // Equity = 5000 + netIncome(8000-3000) = 10000
        result.Equity.Total.Should().Be(10000m);
        result.Equity.Lines.Should().Contain(l => l.AccountName == "Donem Net Kari (Zarari)");
    }

    [Fact]
    public async Task Handle_EmptyJournal_AllSectionTotalsZero()
    {
        // Arrange
        _accountRepoMock.Setup(r => r.GetAllAsync(_tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChartOfAccounts>());

        _journalRepoMock.Setup(r => r.GetByDateRangeAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry>());

        var query = new GetBalanceSheetQuery(_tenantId, DateTime.UtcNow);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Assets.Total.Should().Be(0m);
        result.Liabilities.Total.Should().Be(0m);
        result.Equity.Total.Should().Be(0m);
        result.IsBalanced.Should().BeTrue(); // 0 == 0 + 0
    }

    [Fact]
    public async Task Handle_RevenueExpense_AdjustEquitySection()
    {
        // Arrange — Net loss scenario: expenses > revenue
        var assetAccount = CreateAccount("100", "Kasa", AccountType.Asset);
        var equityAccount = CreateAccount("500", "Sermaye", AccountType.Equity);
        var revenueAccount = CreateAccount("600", "Satis Geliri", AccountType.Revenue);
        var expenseAccount = CreateAccount("770", "Genel Gider", AccountType.Expense);

        _accountRepoMock.Setup(r => r.GetAllAsync(_tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChartOfAccounts> { assetAccount, equityAccount, revenueAccount, expenseAccount });

        // Revenue 2000, Expense 5000 — net loss of 3000
        var entry = CreatePostedEntry(new List<(Guid, decimal, decimal)>
        {
            (assetAccount.Id, 10000m, 0m),
            (equityAccount.Id, 0m, 13000m),
            (revenueAccount.Id, 0m, 2000m),
            (expenseAccount.Id, 5000m, 0m)
        });

        _journalRepoMock.Setup(r => r.GetByDateRangeAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry> { entry });

        var query = new GetBalanceSheetQuery(_tenantId, DateTime.UtcNow);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert — Equity should be adjusted: 13000 + (2000 - 5000) = 10000
        result.Equity.Total.Should().Be(10000m);
        result.Assets.Total.Should().Be(10000m);
        result.IsBalanced.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AssetNormalBalance_IsDebitPositive()
    {
        // Arrange
        var assetAccount = CreateAccount("100", "Kasa", AccountType.Asset);
        var equityAccount = CreateAccount("500", "Sermaye", AccountType.Equity);

        _accountRepoMock.Setup(r => r.GetAllAsync(_tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChartOfAccounts> { assetAccount, equityAccount });

        var entry = CreatePostedEntry(new List<(Guid, decimal, decimal)>
        {
            (assetAccount.Id, 5000m, 0m),
            (equityAccount.Id, 0m, 5000m)
        });

        _journalRepoMock.Setup(r => r.GetByDateRangeAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry> { entry });

        var query = new GetBalanceSheetQuery(_tenantId, DateTime.UtcNow);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert — debit - credit = positive for assets
        result.Assets.Total.Should().Be(5000m);
    }

    [Fact]
    public async Task Handle_LiabilityNormalBalance_IsCreditPositive()
    {
        // Arrange
        var assetAccount = CreateAccount("100", "Kasa", AccountType.Asset);
        var liabilityAccount = CreateAccount("300", "Borc", AccountType.Liability);

        _accountRepoMock.Setup(r => r.GetAllAsync(_tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChartOfAccounts> { assetAccount, liabilityAccount });

        var entry = CreatePostedEntry(new List<(Guid, decimal, decimal)>
        {
            (assetAccount.Id, 5000m, 0m),
            (liabilityAccount.Id, 0m, 5000m)
        });

        _journalRepoMock.Setup(r => r.GetByDateRangeAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry> { entry });

        var query = new GetBalanceSheetQuery(_tenantId, DateTime.UtcNow);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert — liability uses negative of (debit-credit) = credit-debit
        result.Liabilities.Total.Should().Be(5000m);
    }

    [Fact]
    public async Task Handle_OnlyUnpostedEntries_ReturnsZeros()
    {
        // Arrange
        var assetAccount = CreateAccount("100", "Kasa", AccountType.Asset);

        _accountRepoMock.Setup(r => r.GetAllAsync(_tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChartOfAccounts> { assetAccount });

        var unpostedEntry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Unposted");
        unpostedEntry.AddLine(assetAccount.Id, 5000m, 0m);
        unpostedEntry.AddLine(Guid.NewGuid(), 0m, 5000m);
        // Not posted

        _journalRepoMock.Setup(r => r.GetByDateRangeAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry> { unpostedEntry });

        var query = new GetBalanceSheetQuery(_tenantId, DateTime.UtcNow);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Assets.Total.Should().Be(0m);
        result.IsBalanced.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SetsAsOfDate()
    {
        // Arrange
        var asOfDate = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        _accountRepoMock.Setup(r => r.GetAllAsync(_tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChartOfAccounts>());

        _journalRepoMock.Setup(r => r.GetByDateRangeAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry>());

        var query = new GetBalanceSheetQuery(_tenantId, asOfDate);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.AsOfDate.Should().Be(asOfDate);
    }

    [Fact]
    public async Task Handle_AllFiveAccountTypes_CalculatedCorrectly()
    {
        // Arrange — full scenario with all 5 account types
        var asset = CreateAccount("100", "Kasa", AccountType.Asset);
        var liability = CreateAccount("300", "Borc", AccountType.Liability);
        var equity = CreateAccount("500", "Sermaye", AccountType.Equity);
        var revenue = CreateAccount("600", "Gelir", AccountType.Revenue);
        var expense = CreateAccount("770", "Gider", AccountType.Expense);

        _accountRepoMock.Setup(r => r.GetAllAsync(_tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChartOfAccounts> { asset, liability, equity, revenue, expense });

        var entry = CreatePostedEntry(new List<(Guid, decimal, decimal)>
        {
            (asset.Id, 20000m, 0m),
            (liability.Id, 0m, 5000m),
            (equity.Id, 0m, 10000m),
            (revenue.Id, 0m, 7000m),
            (expense.Id, 2000m, 0m)
        });

        _journalRepoMock.Setup(r => r.GetByDateRangeAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry> { entry });

        var query = new GetBalanceSheetQuery(_tenantId, DateTime.UtcNow);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Assets.Total.Should().Be(20000m);
        result.Liabilities.Total.Should().Be(5000m);
        // Equity = 10000 + netIncome(7000-2000) = 15000
        result.Equity.Total.Should().Be(15000m);
        // 20000 == 5000 + 15000
        result.IsBalanced.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SectionNames_AreCorrect()
    {
        // Arrange
        _accountRepoMock.Setup(r => r.GetAllAsync(_tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChartOfAccounts>());

        _journalRepoMock.Setup(r => r.GetByDateRangeAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry>());

        var query = new GetBalanceSheetQuery(_tenantId, DateTime.UtcNow);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Assets.SectionName.Should().Be("Varliklar");
        result.Liabilities.SectionName.Should().Be("Borclar");
        result.Equity.SectionName.Should().Be("Ozkaynaklar");
    }

    [Fact]
    public async Task Handle_AssetSection_ContainsLineItems()
    {
        // Arrange
        var kasa = CreateAccount("100", "Kasa", AccountType.Asset);
        var banka = CreateAccount("102", "Bankalar", AccountType.Asset);
        var equity = CreateAccount("500", "Sermaye", AccountType.Equity);

        _accountRepoMock.Setup(r => r.GetAllAsync(_tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChartOfAccounts> { kasa, banka, equity });

        var entry = CreatePostedEntry(new List<(Guid, decimal, decimal)>
        {
            (kasa.Id, 5000m, 0m),
            (banka.Id, 3000m, 0m),
            (equity.Id, 0m, 8000m)
        });

        _journalRepoMock.Setup(r => r.GetByDateRangeAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry> { entry });

        var query = new GetBalanceSheetQuery(_tenantId, DateTime.UtcNow);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert — Assets section contains line items sorted by code
        result.Assets.Lines.Should().HaveCount(2);
        result.Assets.Lines[0].AccountCode.Should().Be("100");
        result.Assets.Lines[0].Balance.Should().Be(5000m);
        result.Assets.Lines[1].AccountCode.Should().Be("102");
        result.Assets.Lines[1].Balance.Should().Be(3000m);
        result.Assets.Total.Should().Be(8000m);
    }
}
