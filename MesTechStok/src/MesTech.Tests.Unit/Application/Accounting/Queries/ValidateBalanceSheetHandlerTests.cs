using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.ValidateBalanceSheet;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Accounting.Services;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Queries;

/// <summary>
/// ValidateBalanceSheetHandler tests — balance sheet validation (Assets == Liabilities + Equity).
/// Verifies balanced, imbalanced, and empty scenarios using the domain validation service.
/// </summary>
[Trait("Category", "Unit")]
public class ValidateBalanceSheetHandlerTests
{
    private readonly Mock<IChartOfAccountsRepository> _accountRepoMock;
    private readonly Mock<IJournalEntryRepository> _journalRepoMock;
    private readonly BalanceSheetValidationService _validationService;
    private readonly ValidateBalanceSheetHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public ValidateBalanceSheetHandlerTests()
    {
        _accountRepoMock = new Mock<IChartOfAccountsRepository>();
        _journalRepoMock = new Mock<IJournalEntryRepository>();
        _validationService = new BalanceSheetValidationService();

        _sut = new ValidateBalanceSheetHandler(
            _accountRepoMock.Object,
            _journalRepoMock.Object,
            _validationService);
    }

    private ChartOfAccounts CreateAccount(string code, string name, AccountType type)
    {
        return ChartOfAccounts.Create(_tenantId, code, name, type);
    }

    private JournalEntry CreatePostedEntry(List<(Guid accountId, decimal debit, decimal credit)> lines)
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Validation test");
        foreach (var (accountId, debit, credit) in lines)
        {
            entry.AddLine(accountId, debit, credit);
        }
        entry.Post();
        return entry;
    }

    [Fact]
    public async Task Handle_BalancedSheet_ReturnsIsBalancedTrue()
    {
        // Arrange — Assets 10000 == Liabilities 4000 + Equity 6000
        var asset = CreateAccount("100", "Kasa", AccountType.Asset);
        var liability = CreateAccount("300", "Borc", AccountType.Liability);
        var equity = CreateAccount("500", "Sermaye", AccountType.Equity);

        _accountRepoMock
            .Setup(r => r.GetAllAsync(_tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChartOfAccounts> { asset, liability, equity });

        var entry = CreatePostedEntry(new List<(Guid, decimal, decimal)>
        {
            (asset.Id, 10000m, 0m),
            (liability.Id, 0m, 4000m),
            (equity.Id, 0m, 6000m)
        });

        _journalRepoMock
            .Setup(r => r.GetByDateRangeAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry> { entry });

        var query = new ValidateBalanceSheetQuery(_tenantId, DateTime.UtcNow);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsBalanced.Should().BeTrue();
        result.TotalAssets.Should().Be(10000m);
        result.TotalLiabilities.Should().Be(4000m);
        result.TotalEquity.Should().Be(6000m);
        result.Difference.Should().Be(0m);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_EmptyJournal_ReturnsBalancedWithZeros()
    {
        // Arrange
        _accountRepoMock
            .Setup(r => r.GetAllAsync(_tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChartOfAccounts>());

        _journalRepoMock
            .Setup(r => r.GetByDateRangeAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry>());

        var query = new ValidateBalanceSheetQuery(_tenantId, DateTime.UtcNow);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsBalanced.Should().BeTrue();
        result.TotalAssets.Should().Be(0m);
        result.TotalLiabilities.Should().Be(0m);
        result.TotalEquity.Should().Be(0m);
    }

    [Fact]
    public async Task Handle_ImbalancedSheet_ReturnsIsBalancedFalseWithErrors()
    {
        // Arrange — Assets 15000 != Liabilities 4000 + Equity 6000 (missing 5000)
        var asset = CreateAccount("100", "Kasa", AccountType.Asset);
        var liability = CreateAccount("300", "Borc", AccountType.Liability);
        var equity = CreateAccount("500", "Sermaye", AccountType.Equity);

        _accountRepoMock
            .Setup(r => r.GetAllAsync(_tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChartOfAccounts> { asset, liability, equity });

        // Balanced entry
        var entry1 = CreatePostedEntry(new List<(Guid, decimal, decimal)>
        {
            (asset.Id, 10000m, 0m),
            (liability.Id, 0m, 4000m),
            (equity.Id, 0m, 6000m)
        });

        // Imbalanced: asset debited with credit to unknown account
        var otherAccountId = Guid.NewGuid();
        var entry2 = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Extra");
        entry2.AddLine(asset.Id, 5000m, 0m);
        entry2.AddLine(otherAccountId, 0m, 5000m);
        entry2.Post();

        _journalRepoMock
            .Setup(r => r.GetByDateRangeAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry> { entry1, entry2 });

        var query = new ValidateBalanceSheetQuery(_tenantId, DateTime.UtcNow);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsBalanced.Should().BeFalse();
        result.TotalAssets.Should().Be(15000m);
        result.Difference.Should().NotBe(0m);
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_OnlyUnpostedEntries_ReturnsBalancedZeros()
    {
        // Arrange — unposted entries should be excluded
        var asset = CreateAccount("100", "Kasa", AccountType.Asset);

        _accountRepoMock
            .Setup(r => r.GetAllAsync(_tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChartOfAccounts> { asset });

        var unpostedEntry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Draft");
        unpostedEntry.AddLine(asset.Id, 5000m, 0m);
        unpostedEntry.AddLine(Guid.NewGuid(), 0m, 5000m);
        // NOT posted

        _journalRepoMock
            .Setup(r => r.GetByDateRangeAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry> { unpostedEntry });

        var query = new ValidateBalanceSheetQuery(_tenantId, DateTime.UtcNow);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsBalanced.Should().BeTrue();
        result.TotalAssets.Should().Be(0m);
    }
}
