using FluentAssertions;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Accounting.Services;

namespace MesTech.Tests.Unit.Accounting.Services;

[Trait("Category", "Unit")]
public class BalanceSheetValidationServiceTests
{
    private readonly BalanceSheetValidationService _sut = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void ValidateFromData_BalancedAccounts_ShouldReturnBalanced()
    {
        // Asset account with 1000 debit
        var assetAccount = ChartOfAccounts.Create(_tenantId, "100", "Kasa", AccountType.Asset);
        // Equity account with 1000 credit
        var equityAccount = ChartOfAccounts.Create(_tenantId, "500", "Sermaye", AccountType.Equity);

        var accounts = new List<ChartOfAccounts> { assetAccount, equityAccount };

        var lines = new List<JournalLine>
        {
            JournalLine.Create(Guid.NewGuid(), assetAccount.Id, 1000m, 0m),
            JournalLine.Create(Guid.NewGuid(), equityAccount.Id, 0m, 1000m)
        };

        var result = _sut.ValidateFromData(accounts, lines);

        result.IsBalanced.Should().BeTrue();
        result.TotalAssets.Should().Be(1000m);
        result.TotalEquity.Should().Be(1000m);
        result.Difference.Should().Be(0m);
    }

    [Fact]
    public void ValidateFromData_WithRevenue_ShouldAdjustEquity()
    {
        var assetAccount = ChartOfAccounts.Create(_tenantId, "100", "Kasa", AccountType.Asset);
        var equityAccount = ChartOfAccounts.Create(_tenantId, "500", "Sermaye", AccountType.Equity);
        var revenueAccount = ChartOfAccounts.Create(_tenantId, "600", "Satis Geliri", AccountType.Revenue);

        var accounts = new List<ChartOfAccounts> { assetAccount, equityAccount, revenueAccount };

        var lines = new List<JournalLine>
        {
            // Asset: 1500 debit
            JournalLine.Create(Guid.NewGuid(), assetAccount.Id, 1500m, 0m),
            // Equity: 1000 credit
            JournalLine.Create(Guid.NewGuid(), equityAccount.Id, 0m, 1000m),
            // Revenue: 500 credit (net income flows to equity)
            JournalLine.Create(Guid.NewGuid(), revenueAccount.Id, 0m, 500m)
        };

        var result = _sut.ValidateFromData(accounts, lines);

        // Assets = 1500, Liabilities = 0, Equity = 1000, Revenue = 500, Expense = 0
        // Adjusted Equity = 1000 + (500 - 0) = 1500
        result.IsBalanced.Should().BeTrue();
        result.TotalAssets.Should().Be(1500m);
        result.TotalEquity.Should().Be(1500m); // 1000 equity + 500 net income
    }

    [Fact]
    public void ValidateFromData_WithRevenueAndExpense_ShouldComputeNetIncome()
    {
        var assetAccount = ChartOfAccounts.Create(_tenantId, "100", "Kasa", AccountType.Asset);
        var equityAccount = ChartOfAccounts.Create(_tenantId, "500", "Sermaye", AccountType.Equity);
        var revenueAccount = ChartOfAccounts.Create(_tenantId, "600", "Satis Geliri", AccountType.Revenue);
        var expenseAccount = ChartOfAccounts.Create(_tenantId, "770", "Genel Yonetim", AccountType.Expense);

        var accounts = new List<ChartOfAccounts> { assetAccount, equityAccount, revenueAccount, expenseAccount };

        var lines = new List<JournalLine>
        {
            JournalLine.Create(Guid.NewGuid(), assetAccount.Id, 1200m, 0m),
            JournalLine.Create(Guid.NewGuid(), equityAccount.Id, 0m, 1000m),
            JournalLine.Create(Guid.NewGuid(), revenueAccount.Id, 0m, 500m),
            JournalLine.Create(Guid.NewGuid(), expenseAccount.Id, 300m, 0m),
        };

        var result = _sut.ValidateFromData(accounts, lines);

        // Assets = 1200, Equity = 1000, Revenue = 500, Expense = 300
        // Adjusted Equity = 1000 + (500 - 300) = 1200
        result.IsBalanced.Should().BeTrue();
        result.TotalAssets.Should().Be(1200m);
    }

    [Fact]
    public void ValidateFromData_UnbalancedAccounts_ShouldReturnNotBalanced()
    {
        var assetAccount = ChartOfAccounts.Create(_tenantId, "100", "Kasa", AccountType.Asset);
        var equityAccount = ChartOfAccounts.Create(_tenantId, "500", "Sermaye", AccountType.Equity);

        var accounts = new List<ChartOfAccounts> { assetAccount, equityAccount };

        var lines = new List<JournalLine>
        {
            JournalLine.Create(Guid.NewGuid(), assetAccount.Id, 1000m, 0m),
            JournalLine.Create(Guid.NewGuid(), equityAccount.Id, 0m, 500m) // not balanced
        };

        var result = _sut.ValidateFromData(accounts, lines);

        result.IsBalanced.Should().BeFalse();
        result.Difference.Should().NotBe(0m);
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void ValidateFromData_EmptyData_ShouldReturnBalanced()
    {
        var result = _sut.ValidateFromData(
            new List<ChartOfAccounts>(),
            new List<JournalLine>());

        result.IsBalanced.Should().BeTrue();
        result.TotalAssets.Should().Be(0m);
        result.TotalLiabilities.Should().Be(0m);
        result.TotalEquity.Should().Be(0m);
    }

    [Fact]
    public void ValidateFromData_NullAccounts_ShouldThrow()
    {
        var act = () => _sut.ValidateFromData(null!, new List<JournalLine>());

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ValidateFromData_NullLines_ShouldThrow()
    {
        var act = () => _sut.ValidateFromData(new List<ChartOfAccounts>(), null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ValidateFromData_WithLiabilities_ShouldBalance()
    {
        var assetAccount = ChartOfAccounts.Create(_tenantId, "100", "Kasa", AccountType.Asset);
        var liabilityAccount = ChartOfAccounts.Create(_tenantId, "300", "Borclar", AccountType.Liability);
        var equityAccount = ChartOfAccounts.Create(_tenantId, "500", "Sermaye", AccountType.Equity);

        var accounts = new List<ChartOfAccounts> { assetAccount, liabilityAccount, equityAccount };

        var lines = new List<JournalLine>
        {
            JournalLine.Create(Guid.NewGuid(), assetAccount.Id, 2000m, 0m),
            JournalLine.Create(Guid.NewGuid(), liabilityAccount.Id, 0m, 800m),
            JournalLine.Create(Guid.NewGuid(), equityAccount.Id, 0m, 1200m)
        };

        var result = _sut.ValidateFromData(accounts, lines);

        result.IsBalanced.Should().BeTrue();
        result.TotalAssets.Should().Be(2000m);
        result.TotalLiabilities.Should().Be(800m);
        result.TotalEquity.Should().Be(1200m);
    }

    [Fact]
    public void ValidateAsync_ShouldThrowInvalidOperation()
    {
        var act = async () => await _sut.ValidateAsync(_tenantId, DateTime.UtcNow);

        act.Should().ThrowAsync<InvalidOperationException>();
    }
}
