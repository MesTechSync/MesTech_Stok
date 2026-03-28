using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// G093+G099 REGRESYON: 3 GL event handler JournalEntry'yi oluşturuyor
/// ama _journalRepo.AddAsync() ÇAĞIRMIYOR.
///
/// Bu test handler'ın repository'ye AddAsync çağrısı yaptığını doğrular.
/// ŞU AN BAŞARISIZ — bug düzeltildiğinde geçer.
///
/// Etki: TÜM muhasebe GL kayıtları (fatura, komisyon, kargo) DB'ye yazılmıyor.
///       Mali tablolar 0 gösteriyor. Mizan, gelir tablosu, bilanço BOŞ.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Bug", "G093")]
[Trait("Bug", "G099")]
public class GLDataLossRegressionTests
{
    private readonly Mock<IJournalEntryRepository> _journalRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    public GLDataLossRegressionTests()
    {
        _journalRepoMock.Setup(r => r.ExistsByReferenceAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    /// <summary>
    /// G093: InvoiceApprovedGLHandler MUST call _journalRepo.AddAsync(entry).
    /// Currently MISSING — handler creates entry, validates, posts, but never adds to repo.
    /// </summary>
    [Fact(DisplayName = "G093: InvoiceApprovedGL must AddAsync journal entry to repository")]
    public async Task InvoiceApprovedGL_MustCallAddAsync()
    {
        var sut = new InvoiceApprovedGLHandler(
            _uowMock.Object, _journalRepoMock.Object,
            Mock.Of<ILogger<InvoiceApprovedGLHandler>>());

        await sut.HandleAsync(
            Guid.NewGuid(), _tenantId, "INV-001",
            1180m, 1000m, 180m, CancellationToken.None);

        // G093 BUG: Bu assertion ŞU AN BAŞARISIZ çünkü AddAsync çağrılmıyor
        _journalRepoMock.Verify(
            r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "G093: InvoiceApprovedGLHandler MUST call _journalRepo.AddAsync(entry) " +
            "to persist GL entry to database. Without this, ALL invoice GL records are LOST.");
    }

    /// <summary>
    /// G093: CommissionChargedGLHandler MUST call _journalRepo.AddAsync(entry).
    /// </summary>
    [Fact(DisplayName = "G093: CommissionChargedGL must AddAsync journal entry")]
    public async Task CommissionChargedGL_MustCallAddAsync()
    {
        var sut = new CommissionChargedGLHandler(
            _uowMock.Object, _journalRepoMock.Object,
            Mock.Of<ILogger<CommissionChargedGLHandler>>());

        await sut.HandleAsync(
            Guid.NewGuid(), _tenantId, PlatformType.Trendyol,
            100m, 0.10m, CancellationToken.None);

        _journalRepoMock.Verify(
            r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "G093: CommissionChargedGLHandler MUST call AddAsync. " +
            "Commission GL entries are LOST without this.");
    }

    /// <summary>
    /// G093: OrderShippedCostHandler MUST call _journalRepo.AddAsync(entry).
    /// </summary>
    [Fact(DisplayName = "G093: OrderShippedCostGL must AddAsync journal entry")]
    public async Task OrderShippedCostGL_MustCallAddAsync()
    {
        var sut = new OrderShippedCostHandler(
            _uowMock.Object, _journalRepoMock.Object,
            Mock.Of<ILogger<OrderShippedCostHandler>>());

        await sut.HandleAsync(
            Guid.NewGuid(), _tenantId, "TRK-001",
            CargoProvider.YurticiKargo, 25.50m, CancellationToken.None);

        _journalRepoMock.Verify(
            r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "G093: OrderShippedCostHandler MUST call AddAsync. " +
            "Cargo expense GL entries are LOST without this.");
    }
}
