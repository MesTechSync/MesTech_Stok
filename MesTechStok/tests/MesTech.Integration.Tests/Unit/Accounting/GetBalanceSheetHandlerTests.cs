using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetBalanceSheet;
using MesTech.Application.Interfaces.Accounting;
using IJournalEntryRepository = MesTech.Application.Interfaces.Accounting.IJournalEntryRepository;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

/// <summary>
/// GetBalanceSheetHandler: bilanço raporu — muhasebe denklemi doğrulaması.
/// Aktif = Pasif + Özkaynak olmalı (IsBalanced = true).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Handler")]
[Trait("Group", "AccountingReport")]
public class GetBalanceSheetHandlerTests
{
    private readonly Mock<IChartOfAccountsRepository> _accountRepo = new();
    private readonly Mock<IJournalEntryRepository> _journalRepo = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private GetBalanceSheetHandler CreateHandler() =>
        new(_accountRepo.Object, _journalRepo.Object);

    [Fact]
    public async Task Handle_NoAccounts_ReturnsEmptyBalanced()
    {
        _accountRepo.Setup(r => r.GetAllAsync(_tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChartOfAccounts>());
        _journalRepo.Setup(r => r.GetByDateRangeAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry>());

        var handler = CreateHandler();
        var result = await handler.Handle(
            new GetBalanceSheetQuery(_tenantId, DateTime.UtcNow), CancellationToken.None);

        result.Assets.Lines.Should().BeEmpty();
        result.Liabilities.Lines.Should().BeEmpty();
        result.IsBalanced.Should().BeTrue(); // 0 = 0 + 0
    }

    [Fact]
    public async Task Handle_WithEntries_ReturnsBalancedSheet()
    {
        // Hesap planı: 100 Kasa (Asset), 300 Sermaye (Equity), 600 Satış (Revenue)
        var kasa = ChartOfAccounts.Create(_tenantId, "100", "Kasa", AccountType.Asset);
        var sermaye = ChartOfAccounts.Create(_tenantId, "300", "Sermaye", AccountType.Equity);
        var satis = ChartOfAccounts.Create(_tenantId, "600", "Satışlar", AccountType.Revenue);

        _accountRepo.Setup(r => r.GetAllAsync(_tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChartOfAccounts> { kasa, sermaye, satis });

        // Yevmiye: 10000 TL sermaye + 5000 TL satış
        var entry1 = JournalEntry.Create(_tenantId, DateTime.UtcNow.AddDays(-10), "Sermaye", "JE-001");
        entry1.AddLine(kasa.Id, 10000m, 0m, "Kasa borç");
        entry1.AddLine(sermaye.Id, 0m, 10000m, "Sermaye alacak");
        entry1.Validate();
        entry1.Post();

        var entry2 = JournalEntry.Create(_tenantId, DateTime.UtcNow.AddDays(-5), "Satış", "JE-002");
        entry2.AddLine(kasa.Id, 5000m, 0m, "Kasa borç");
        entry2.AddLine(satis.Id, 0m, 5000m, "Satış alacak");
        entry2.Validate();
        entry2.Post();

        _journalRepo.Setup(r => r.GetByDateRangeAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry> { entry1, entry2 });

        var handler = CreateHandler();
        var result = await handler.Handle(
            new GetBalanceSheetQuery(_tenantId, DateTime.UtcNow), CancellationToken.None);

        // Muhasebe denklemi: Aktif = Pasif + Özkaynak
        result.IsBalanced.Should().BeTrue();
        result.Assets.Total.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Handle_NullRequest_Throws()
    {
        var handler = CreateHandler();
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            handler.Handle(null!, CancellationToken.None));
    }
}
