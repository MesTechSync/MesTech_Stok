using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetTrialBalance;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

/// <summary>
/// Z14: Mizan (Trial Balance) handler testi.
/// Kritik muhasebe kuralı: toplam borç = toplam alacak.
/// Açılış + dönem = kapanış bakiyesi.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Handler")]
[Trait("Group", "AccountingChain")]
public class GetTrialBalanceHandlerTests
{
    private readonly Mock<IChartOfAccountsRepository> _accountRepo = new();
    private readonly Mock<IJournalEntryRepository> _journalRepo = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private GetTrialBalanceHandler CreateHandler() =>
        new(_accountRepo.Object, _journalRepo.Object);

    [Fact]
    public async Task Handle_EmptyAccounts_ReturnsEmptyLines()
    {
        _accountRepo.Setup(r => r.GetAllAsync(_tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChartOfAccounts>());
        _journalRepo.Setup(r => r.GetByDateRangeAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry>());

        var handler = CreateHandler();
        var query = new GetTrialBalanceQuery(_tenantId, new DateTime(2026, 1, 1), new DateTime(2026, 3, 31));

        var result = await handler.Handle(query, CancellationToken.None);

        result.Lines.Should().BeEmpty();
        result.GrandTotalOpeningDebit.Should().Be(0);
        result.GrandTotalClosingDebit.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithEntries_CalculatesCorrectBalances()
    {
        var kasaAccount = ChartOfAccounts.Create(_tenantId, "100", "Kasa", AccountType.Asset);
        var satisAccount = ChartOfAccounts.Create(_tenantId, "600", "Satışlar", AccountType.Revenue);

        _accountRepo.Setup(r => r.GetAllAsync(_tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChartOfAccounts> { kasaAccount, satisAccount });

        // Dönem kaydı: 1000 TL satış — Kasa borç, Satışlar alacak
        var entry = JournalEntry.Create(_tenantId, new DateTime(2026, 2, 15), "Satış", "JE-001");
        entry.AddLine(kasaAccount.Id, 1000m, 0m, "Kasa borç");
        entry.AddLine(satisAccount.Id, 0m, 1000m, "Satış alacak");
        entry.Validate();
        entry.Post();

        // Açılış dönemi: entry yok
        _journalRepo.Setup(r => r.GetByDateRangeAsync(_tenantId, It.IsAny<DateTime>(), It.Is<DateTime>(d => d < new DateTime(2026, 1, 1)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry>());
        // Dönem: entry var
        _journalRepo.Setup(r => r.GetByDateRangeAsync(_tenantId, It.Is<DateTime>(d => d >= new DateTime(2026, 1, 1)), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry> { entry });

        var handler = CreateHandler();
        var query = new GetTrialBalanceQuery(_tenantId, new DateTime(2026, 1, 1), new DateTime(2026, 3, 31));

        var result = await handler.Handle(query, CancellationToken.None);

        result.Lines.Should().HaveCount(2);
        // Muhasebe kuralı: toplam borç = toplam alacak
        result.GrandTotalPeriodDebit.Should().Be(result.GrandTotalPeriodCredit);
        result.GrandTotalClosingDebit.Should().Be(result.GrandTotalClosingCredit);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNull()
    {
        var handler = CreateHandler();
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            handler.Handle(null!, CancellationToken.None));
    }
}
