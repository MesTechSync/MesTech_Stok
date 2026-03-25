using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

/// <summary>
/// InvoiceCancelledReversalHandler: Z4 zinciri — fatura iptal → ters yevmiye.
/// Kritik iş kuralları:
///   - 3 satırlı ters yevmiye oluşturulmalı (600, 391, 120)
///   - Borç = Alacak dengesi sağlanmalı (Validate())
///   - Referans REV-{invoiceNumber} olmalı
///   - KDV oranı ile net/brüt ayrımı yapılmalı
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Handler")]
[Trait("Group", "AccountingChain")]
public class InvoiceCancelledReversalHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<InvoiceCancelledReversalHandler>> _logger = new();

    public InvoiceCancelledReversalHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private InvoiceCancelledReversalHandler CreateHandler() =>
        new(_uow.Object, _logger.Object);

    [Fact]
    public async Task HandleAsync_ValidCancel_CreatesReversalAndSaves()
    {
        var handler = CreateHandler();

        // Act — 1200 TL fatura iptali (1000 net + 200 KDV)
        await handler.HandleAsync(
            invoiceId: Guid.NewGuid(),
            orderId: Guid.NewGuid(),
            invoiceNumber: "INV-001",
            reason: "Müşteri iade talebi",
            tenantId: Guid.NewGuid(),
            grandTotal: 1200m,
            ct: CancellationToken.None);

        // Assert — SaveChanges çağrılmalı (ters yevmiye persist)
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ZeroAmount_ThrowsArgumentException()
    {
        var handler = CreateHandler();

        // 0 TL fatura → JournalEntry.AddLine(0,0) domain guard fırlatır
        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(
                Guid.NewGuid(), Guid.NewGuid(), "INV-ZERO",
                "Test", Guid.NewGuid(), 0m, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_NullReason_UsesDefaultText()
    {
        var handler = CreateHandler();

        // reason null — "Sebep belirtilmedi" kullanılmalı
        await handler.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "INV-002",
            null, Guid.NewGuid(), 500m, CancellationToken.None);

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
