using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Domain.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using IUnitOfWork = MesTech.Domain.Interfaces.IUnitOfWork;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.EventHandlers;

/// <summary>
/// WithholdingTaxGLHandler — Zincir 7: BORÇ 193 Peşin Öd.Vergi / ALACAK 120.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class WithholdingTaxGLHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IJournalEntryRepository> _journalRepo = new();
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public WithholdingTaxGLHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _journalRepo.Setup(r => r.ExistsByReferenceAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    private WithholdingTaxGLHandler CreateSut() => new(
        _uow.Object, _journalRepo.Object, Mock.Of<ILogger<WithholdingTaxGLHandler>>());

    [Fact]
    public async Task Handle_ShouldCreateDebit193Credit120()
    {
        JournalEntry? captured = null;
        _journalRepo.Setup(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()))
            .Callback<JournalEntry, CancellationToken>((je, _) => captured = je).Returns(Task.CompletedTask);

        await CreateSut().HandleAsync(Guid.NewGuid(), TenantId,
            10000m, 0.01m, 100m, "GelirVergisi", CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Lines.Should().Contain(l =>
            l.AccountId == AccountingConstants.Account193PrepaidTax && l.Debit == 100m,
            "BORÇ 193 = stopaj tutarı");
        captured.Lines.Should().Contain(l =>
            l.AccountId == AccountingConstants.Account120Receivables && l.Credit == 100m,
            "ALACAK 120 = stopaj tutarı");
    }

    [Fact]
    public async Task Handle_DebitEqualsCredit()
    {
        JournalEntry? captured = null;
        _journalRepo.Setup(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()))
            .Callback<JournalEntry, CancellationToken>((je, _) => captured = je).Returns(Task.CompletedTask);

        await CreateSut().HandleAsync(Guid.NewGuid(), TenantId,
            50000m, 0.01m, 500m, "GelirVergisi", CancellationToken.None);

        captured!.Lines.Sum(l => l.Debit).Should().Be(captured.Lines.Sum(l => l.Credit));
    }

    [Fact]
    public async Task Handle_ZeroWithholding_ShouldSkip()
    {
        await CreateSut().HandleAsync(Guid.NewGuid(), TenantId,
            10000m, 0.01m, 0m, "GelirVergisi", CancellationToken.None);

        _journalRepo.Verify(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Idempotent_ShouldSkipDuplicate()
    {
        var whtId = Guid.NewGuid();
        _journalRepo.Setup(r => r.ExistsByReferenceAsync(TenantId,
                $"WHT-{whtId.ToString("N")[..12]}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await CreateSut().HandleAsync(whtId, TenantId,
            10000m, 0.01m, 100m, "GelirVergisi", CancellationToken.None);

        _journalRepo.Verify(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldPostEntry()
    {
        JournalEntry? captured = null;
        _journalRepo.Setup(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()))
            .Callback<JournalEntry, CancellationToken>((je, _) => captured = je).Returns(Task.CompletedTask);

        await CreateSut().HandleAsync(Guid.NewGuid(), TenantId,
            20000m, 0.01m, 200m, "GelirVergisi", CancellationToken.None);

        captured!.IsPosted.Should().BeTrue("entry should be posted after creation");
    }
}
