using FluentAssertions;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.EventHandlers;
using MesTech.Application.Features.Accounting.Queries.GetTrialBalance;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.EventHandlers;

// ════════════════════════════════════════════════════════
// DEV5: Kalan zincir iş mantığı testleri — Z3, Z11, Z14
// Z3:  Fatura → GL yevmiye (120/600/391)
// Z11: Gecikmiş sipariş → bildirim
// Z14: Mizan raporu (borç = alacak)
// ════════════════════════════════════════════════════════

#region Z3: InvoiceApprovedGL

[Trait("Category", "Unit")]
[Trait("Layer", "Chain")]
public class InvoiceApprovedGLBusinessTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IJournalEntryRepository> _journalRepo = new();
    private readonly Mock<ILogger<InvoiceApprovedGLHandler>> _logger = new();

    private InvoiceApprovedGLHandler CreateSut() =>
        new(_uow.Object, _journalRepo.Object, _logger.Object);

    [Fact]
    public async Task Handle_ZeroGrandTotal_ShouldSkipGLEntry()
    {
        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "INV-001", 0m, 0m, 0m, CancellationToken.None);
        _journalRepo.Verify(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicateInvoice_ShouldSkipIdempotent()
    {
        _journalRepo.Setup(r => r.ExistsByReferenceAsync(It.IsAny<Guid>(), "INV-DUP", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "INV-DUP", 1000m, 180m, 820m, CancellationToken.None);
        _journalRepo.Verify(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidInvoice_ShouldCreateJournalAndSave()
    {
        _journalRepo.Setup(r => r.ExistsByReferenceAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "INV-003", 1180m, 180m, 1000m, CancellationToken.None);

        _journalRepo.Verify(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region Z11: StaleOrderNotification

[Trait("Category", "Unit")]
[Trait("Layer", "Chain")]
public class StaleOrderNotificationBusinessTests
{
    private readonly Mock<INotificationLogRepository> _notifRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<StaleOrderNotificationHandler>> _logger = new();

    private StaleOrderNotificationHandler CreateSut() =>
        new(_notifRepo.Object, _uow.Object, _logger.Object);

    [Fact]
    public async Task Handle_TrendyolStaleOrder_ShouldCreateNotification()
    {
        NotificationLog? captured = null;
        _notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationLog, CancellationToken>((n, _) => captured = n);

        var sut = CreateSut();
        await sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "ORD-TR-001",
            PlatformType.Trendyol, TimeSpan.FromHours(52),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Content.Should().Contain("ORD-TR-001");
        captured.Content.Should().Contain("52");
        captured.Content.Should().Contain("Trendyol");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AmazonStaleOrder_ShouldIncludePlatformName()
    {
        NotificationLog? captured = null;
        _notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationLog, CancellationToken>((n, _) => captured = n);

        var sut = CreateSut();
        await sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "ORD-AMZ-001",
            PlatformType.Amazon, TimeSpan.FromHours(26),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Content.Should().Contain("Amazon");
    }

    [Fact]
    public async Task Handle_NullPlatform_ShouldShowBilinmiyor()
    {
        NotificationLog? captured = null;
        _notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationLog, CancellationToken>((n, _) => captured = n);

        var sut = CreateSut();
        await sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "ORD-UNK-001",
            null, TimeSpan.FromHours(72),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Content.Should().Contain("Bilinmiyor");
    }
}

#endregion

#region Z14: TrialBalance (Mizan)

[Trait("Category", "Unit")]
[Trait("Layer", "Chain")]
public class TrialBalanceBusinessTests
{
    private readonly Mock<IChartOfAccountsRepository> _accountRepo = new();
    private readonly Mock<IJournalEntryRepository> _journalRepo = new();

    private GetTrialBalanceHandler CreateSut() =>
        new(_accountRepo.Object, _journalRepo.Object);

    [Fact]
    public async Task Handle_EmptyAccounts_ShouldReturnEmptyTrialBalance()
    {
        _accountRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChartOfAccounts>());
        _journalRepo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry>());

        var sut = CreateSut();
        var query = new GetTrialBalanceQuery(Guid.NewGuid(), DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);
        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Lines.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrow()
    {
        var sut = CreateSut();
        var act = async () => await sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

#endregion
