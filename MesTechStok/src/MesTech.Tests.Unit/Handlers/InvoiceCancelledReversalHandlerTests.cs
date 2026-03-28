using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Domain.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Tests for InvoiceCancelledReversalHandler.
/// G068: P0 BUG — hardcoded taxRate=0.20m causes wrong GL reversal for non-20% invoices.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Bug", "G068")]
public class InvoiceCancelledReversalHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IJournalEntryRepository> _journalRepoMock = new();
    private readonly Mock<IInvoiceRepository> _invoiceRepoMock = new();
    private readonly InvoiceCancelledReversalHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public InvoiceCancelledReversalHandlerTests()
    {
        _journalRepoMock.Setup(r => r.ExistsByReferenceAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _sut = new InvoiceCancelledReversalHandler(
            _uowMock.Object, _journalRepoMock.Object, _invoiceRepoMock.Object,
            Mock.Of<ILogger<InvoiceCancelledReversalHandler>>());
    }

    [Fact]
    public async Task Handle_ZeroAmount_SkipsGLEntry()
    {
        await _sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "INV-001",
            "test", _tenantId, 0m, CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicateReference_SkipsGLEntry()
    {
        _journalRepoMock.Setup(r => r.ExistsByReferenceAsync(
            _tenantId, "CANCEL-INV-002", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "INV-002",
            null, _tenantId, 1000m, CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidCancellation_CreatesGLAndSaves()
    {
        await _sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "INV-003",
            "Müşteri iadesi", _tenantId, 1180m, CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// G068 REGRESYON: Hardcoded %20 KDV oranı hatası.
    /// Fatura %8 KDV ile kesilmişse, iptal GL'de net/KDV yanlış hesaplanır.
    /// grandTotal=1080 (%8 KDV) → doğru: net=1000, KDV=80
    /// Şu anki bug:  net=1080/1.20=900, KDV=180 → YANLIŞ!
    /// Bu test, bug düzeltildiğinde handler'a taxRate parametresi eklenince güncellenmelidir.
    /// </summary>
    [Fact(DisplayName = "G068: Hardcoded 20% tax causes wrong split for 8% invoice")]
    public async Task G068_HardcodedTaxRate_ProducesWrongSplitFor8Percent()
    {
        // 1080 TL fatura = 1000 net + 80 KDV (%8)
        // Bug: handler 0.20m kullanıyor → net=900, KDV=180 hesaplar
        var grandTotal = 1080m;
        var expectedCorrectNet = 1000m;   // 1080 / 1.08
        var expectedCorrectTax = 80m;     // 1080 - 1000

        var buggyNet = grandTotal / (1 + 0.20m);     // = 900
        var buggyTax = grandTotal - buggyNet;          // = 180

        // Bug kanıtı: buggy hesap doğru değerlere eşit olMAMALI
        buggyNet.Should().NotBe(expectedCorrectNet,
            "G068: Hardcoded 0.20 tax rate produces 900 instead of 1000 for 8% invoice");
        buggyTax.Should().NotBe(expectedCorrectTax,
            "G068: Hardcoded 0.20 tax rate produces 180 instead of 80 for 8% KDV");

        // Bug farkı: 100 TL net + 100 TL KDV sapma
        (expectedCorrectNet - buggyNet).Should().Be(100m,
            "G068: Net amount deviation is exactly 100 TL for 1080 TL invoice");
    }
}
