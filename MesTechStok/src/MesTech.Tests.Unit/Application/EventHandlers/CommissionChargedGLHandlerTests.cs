using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Domain.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using IUnitOfWork = MesTech.Domain.Interfaces.IUnitOfWork;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.EventHandlers;

/// <summary>
/// CommissionChargedGLHandler — Zincir 6: BORÇ 760 Komisyon + 191 KDV / ALACAK 120.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class CommissionChargedGLHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IJournalEntryRepository> _journalRepo = new();
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public CommissionChargedGLHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _journalRepo.Setup(r => r.ExistsByReferenceAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    private CommissionChargedGLHandler CreateSut() => new(
        _uow.Object, _journalRepo.Object, Mock.Of<ILogger<CommissionChargedGLHandler>>());

    [Fact]
    public async Task Handle_ShouldCreateDebit760Debit191Credit120()
    {
        JournalEntry? captured = null;
        _journalRepo.Setup(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()))
            .Callback<JournalEntry, CancellationToken>((je, _) => captured = je).Returns(Task.CompletedTask);

        await CreateSut().HandleAsync(Guid.NewGuid(), TenantId, PlatformType.Trendyol,
            150m, 0.15m, CancellationToken.None);

        captured.Should().NotBeNull();
        // BORÇ 760 = 150 (komisyon)
        captured!.Lines.Should().Contain(l =>
            l.AccountId == AccountingConstants.Account760MarketingExpenses && l.Debit == 150m);
        // BORÇ 191 = 30 (komisyon KDV %20)
        captured.Lines.Should().Contain(l =>
            l.AccountId == AccountingConstants.Account191VatReceivable && l.Debit == 30m);
        // ALACAK 120 = 180 (komisyon + KDV)
        captured.Lines.Should().Contain(l =>
            l.AccountId == AccountingConstants.Account120Receivables && l.Credit == 180m);
    }

    [Fact]
    public async Task Handle_DebitEqualsCredit()
    {
        JournalEntry? captured = null;
        _journalRepo.Setup(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()))
            .Callback<JournalEntry, CancellationToken>((je, _) => captured = je).Returns(Task.CompletedTask);

        await CreateSut().HandleAsync(Guid.NewGuid(), TenantId, PlatformType.Hepsiburada,
            300m, 0.12m, CancellationToken.None);

        var d = captured!.Lines.Sum(l => l.Debit);
        var c = captured.Lines.Sum(l => l.Credit);
        d.Should().Be(c, "double-entry: debit = credit");
    }

    [Fact]
    public async Task Handle_ZeroCommission_ShouldSkip()
    {
        await CreateSut().HandleAsync(Guid.NewGuid(), TenantId, PlatformType.N11,
            0m, 0m, CancellationToken.None);

        _journalRepo.Verify(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Idempotent_ShouldSkipDuplicate()
    {
        var orderId = Guid.NewGuid();
        _journalRepo.Setup(r => r.ExistsByReferenceAsync(TenantId,
                $"COM-{orderId.ToString()[..8]}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await CreateSut().HandleAsync(orderId, TenantId, PlatformType.Trendyol,
            150m, 0.15m, CancellationToken.None);

        _journalRepo.Verify(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_KdvCalculation_ShouldBe20Percent()
    {
        JournalEntry? captured = null;
        _journalRepo.Setup(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()))
            .Callback<JournalEntry, CancellationToken>((je, _) => captured = je).Returns(Task.CompletedTask);

        await CreateSut().HandleAsync(Guid.NewGuid(), TenantId, PlatformType.Trendyol,
            500m, 0.10m, CancellationToken.None);

        // KDV = 500 * 0.20 = 100
        captured!.Lines.First(l => l.AccountId == AccountingConstants.Account191VatReceivable)
            .Debit.Should().Be(100m, "commission KDV = 20% of commission amount");
        // 120 credit = 500 + 100 = 600
        captured.Lines.First(l => l.AccountId == AccountingConstants.Account120Receivables)
            .Credit.Should().Be(600m);
    }
}
