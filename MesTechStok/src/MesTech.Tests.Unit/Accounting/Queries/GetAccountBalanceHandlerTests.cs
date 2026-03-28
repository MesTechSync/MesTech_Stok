using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetAccountBalance;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Queries;

/// <summary>
/// GetAccountBalanceHandler tests — bakiye hesaplama ve bulunamayan hesap senaryosu.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class GetAccountBalanceHandlerTests
{
    private readonly Mock<IChartOfAccountsRepository> _accountRepoMock;
    private readonly Mock<IJournalEntryRepository> _journalRepoMock;
    private readonly GetAccountBalanceHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetAccountBalanceHandlerTests()
    {
        _accountRepoMock = new Mock<IChartOfAccountsRepository>();
        _journalRepoMock = new Mock<IJournalEntryRepository>();
        _sut = new GetAccountBalanceHandler(_accountRepoMock.Object, _journalRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ValidQuery_ReturnsAccountBalanceWithAggregatedAmounts()
    {
        // Arrange
        var account = ChartOfAccounts.Create(_tenantId, "100", "Kasa", AccountType.Asset);
        var query = new GetAccountBalanceQuery(_tenantId, account.Id);

        // Build a posted journal entry with lines referencing this account
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Test entry", "REF-001");
        entry.AddLine(account.Id, debit: 1000m, credit: 0m, "Borc satiri");
        entry.AddLine(account.Id, debit: 0m, credit: 300m, "Alacak satiri");

        _accountRepoMock
            .Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _journalRepoMock
            .Setup(r => r.GetByAccountIdAsync(_tenantId, account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry> { entry }.AsReadOnly());

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.AccountId.Should().Be(account.Id);
        result.Code.Should().Be("100");
        result.Name.Should().Be("Kasa");
        result.TotalDebit.Should().Be(1000m);
        result.TotalCredit.Should().Be(300m);
        result.Balance.Should().Be(700m);
    }

    [Fact]
    public async Task Handle_AccountNotFound_ReturnsNull()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        var query = new GetAccountBalanceQuery(_tenantId, missingId);

        _accountRepoMock
            .Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChartOfAccounts?)null);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();

        _journalRepoMock.Verify(
            r => r.GetByAccountIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }
}
