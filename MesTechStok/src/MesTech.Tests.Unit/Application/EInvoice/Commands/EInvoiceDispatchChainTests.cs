using FluentAssertions;
using MesTech.Application.Features.EInvoice.Commands;
using MesTech.Application.Features.EInvoice.Queries;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities.EInvoice;
using MesTech.Domain.Enums;
using MesTech.Domain.Exceptions;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.EInvoice.Commands;

[Trait("Category", "Unit")]
[Trait("Feature", "EInvoice")]
public class EInvoiceDispatchChainTests
{
    private readonly Mock<IEInvoiceDocumentRepository> _repoMock = new();
    private readonly Mock<IEInvoiceProvider> _providerMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ILogger<SendEInvoiceHandler>> _loggerMock = new();

    private SendEInvoiceHandler CreateSendHandler() =>
        new(_repoMock.Object, _providerMock.Object, _uowMock.Object, _loggerMock.Object);

    private CancelEInvoiceHandler CreateCancelHandler() =>
        new(_repoMock.Object, _providerMock.Object, _uowMock.Object);

    private static EInvoiceDocument CreateDocument(
        EInvoiceScenario scenario = EInvoiceScenario.TICARIFATURA,
        EInvoiceType type = EInvoiceType.SATIS)
    {
        return EInvoiceDocument.Create(
            gibUuid: Guid.NewGuid().ToString(),
            ettnNo: $"GGB2026CHAIN{DateTime.UtcNow.Ticks % 100000000:D8}",
            scenario: scenario,
            type: type,
            issueDate: DateTime.UtcNow,
            sellerVkn: "0000000000",
            sellerTitle: "MesTech",
            buyerTitle: "Test Alici",
            providerId: "sovos",
            createdBy: "system");
    }

    // ── 1. Send TICARIFATURA scenario passes through to provider ──

    [Fact]
    public async Task Send_TICARIFATURA_ShouldPassScenarioToProvider()
    {
        // Arrange
        var doc = CreateDocument(EInvoiceScenario.TICARIFATURA);
        _repoMock.Setup(r => r.GetByIdAsync(doc.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);
        _providerMock.Setup(p => p.SendAsync(doc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EInvoiceSendResult(true, "REF-TIC-001", null, 1));

        // Act
        await CreateSendHandler().Handle(new SendEInvoiceCommand(doc.Id), CancellationToken.None);

        // Assert — provider received the exact document with TICARIFATURA scenario
        _providerMock.Verify(p => p.SendAsync(
            It.Is<EInvoiceDocument>(d => d.Scenario == EInvoiceScenario.TICARIFATURA),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── 2. Send EARSIVFATURA scenario passes through to provider ──

    [Fact]
    public async Task Send_EARSIVFATURA_ShouldPassScenarioToProvider()
    {
        // Arrange
        var doc = CreateDocument(EInvoiceScenario.EARSIVFATURA);
        _repoMock.Setup(r => r.GetByIdAsync(doc.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);
        _providerMock.Setup(p => p.SendAsync(doc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EInvoiceSendResult(true, "REF-EAR-001", null, 1));

        // Act
        await CreateSendHandler().Handle(new SendEInvoiceCommand(doc.Id), CancellationToken.None);

        // Assert — provider received the exact document with EARSIVFATURA scenario
        _providerMock.Verify(p => p.SendAsync(
            It.Is<EInvoiceDocument>(d => d.Scenario == EInvoiceScenario.EARSIVFATURA),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── 3. Successful send transitions Draft → Sent ──

    [Fact]
    public async Task Send_SuccessfulSend_ShouldTransitionDraftToSent()
    {
        // Arrange
        var doc = CreateDocument();
        doc.Status.Should().Be(EInvoiceStatus.Draft); // precondition
        _repoMock.Setup(r => r.GetByIdAsync(doc.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);
        _providerMock.Setup(p => p.SendAsync(doc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EInvoiceSendResult(true, "REF-STATUS-001", null, 1));

        // Act
        var result = await CreateSendHandler().Handle(new SendEInvoiceCommand(doc.Id), CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        doc.Status.Should().Be(EInvoiceStatus.Sent);
    }

    // ── 4. ProviderRef persisted on document after send ──

    [Fact]
    public async Task Send_ProviderReturnsRef_ShouldPersistProviderRef()
    {
        // Arrange
        var doc = CreateDocument();
        const string expectedRef = "SOVOS-2026-XYZ";
        _repoMock.Setup(r => r.GetByIdAsync(doc.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);
        _providerMock.Setup(p => p.SendAsync(doc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EInvoiceSendResult(true, expectedRef, null, 1));

        // Act
        await CreateSendHandler().Handle(new SendEInvoiceCommand(doc.Id), CancellationToken.None);

        // Assert
        doc.ProviderRef.Should().Be(expectedRef);
        _repoMock.Verify(r => r.UpdateAsync(
            It.Is<EInvoiceDocument>(d => d.ProviderRef == expectedRef),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── 5. Cancel accepted invoice throws BusinessRuleException (domain rule) ──

    [Fact]
    public void Cancel_AcceptedInvoice_ShouldThrowBusinessRule()
    {
        // Arrange — create doc, mark sent, then force Accepted via reflection
        var doc = CreateDocument();
        doc.MarkAsSent("PROV-REF-ACC", creditUsed: 1);

        // No public MarkAsAccepted method — use reflection to reach Accepted state
        typeof(EInvoiceDocument)
            .GetProperty(nameof(EInvoiceDocument.Status))!
            .GetSetMethod(true)!
            .Invoke(doc, [EInvoiceStatus.Accepted]);

        // Act — domain Cancel rejects Accepted invoices
        var act = () => doc.Cancel("Test iptal", cancelledBy: "system");

        // Assert
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*Kabul edilmis fatura iptal edilemez*");
    }

    // ── 6. Cancel sent with ProviderRef — full happy path ──

    [Fact]
    public async Task Cancel_SentWithProviderRef_ProviderSucceeds_ShouldTransitionToCancelled()
    {
        // Arrange
        var doc = CreateDocument();
        doc.MarkAsSent("PROV-REF-CANCEL-OK", creditUsed: 1);
        _repoMock.Setup(r => r.GetByIdAsync(doc.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);
        _providerMock.Setup(p => p.CancelAsync("PROV-REF-CANCEL-OK", "Musteri talebi", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await CreateCancelHandler().Handle(
            new CancelEInvoiceCommand(doc.Id, "Musteri talebi"), CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        doc.Status.Should().Be(EInvoiceStatus.Cancelled);
        _repoMock.Verify(r => r.UpdateAsync(doc, It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── 7. Cancel sent with ProviderRef — provider fails, no local transition ──

    [Fact]
    public async Task Cancel_SentWithProviderRef_ProviderFails_ShouldNotTransitionLocally()
    {
        // Arrange
        var doc = CreateDocument();
        doc.MarkAsSent("PROV-REF-CANCEL-FAIL", creditUsed: 1);
        _repoMock.Setup(r => r.GetByIdAsync(doc.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);
        _providerMock.Setup(p => p.CancelAsync("PROV-REF-CANCEL-FAIL", "Hata testi", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await CreateCancelHandler().Handle(
            new CancelEInvoiceCommand(doc.Id, "Hata testi"), CancellationToken.None);

        // Assert — status stays Sent, no update persisted
        result.Should().BeFalse();
        doc.Status.Should().Be(EInvoiceStatus.Sent);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<EInvoiceDocument>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── 8. CheckVkn — e-Invoice mukellef returns both flags correctly ──

    [Fact]
    public async Task CheckVkn_EInvoiceMukellef_ShouldReturnBothFlags()
    {
        // Arrange
        const string vkn = "1234567890";
        var expected = new VknMukellefResult(
            vkn, IsEInvoiceMukellef: true, IsEArchiveMukellef: false,
            Title: "Alici Ticaret A.S.", CheckedAt: DateTime.UtcNow);

        _providerMock
            .Setup(p => p.CheckVknMukellefAsync(vkn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new CheckVknMukellefHandler(_providerMock.Object);

        // Act
        var result = await handler.Handle(new CheckVknMukellefQuery(vkn), CancellationToken.None);

        // Assert — both flags mapped independently
        result.IsEInvoiceMukellef.Should().BeTrue();
        result.IsEArchiveMukellef.Should().BeFalse();
        result.Title.Should().Be("Alici Ticaret A.S.");
        result.Vkn.Should().Be(vkn);
    }

    // ── 9. Send credit used is recorded on document ──

    [Fact]
    public async Task Send_CreditUsed_ShouldBeRecordedOnDocument()
    {
        // Arrange
        var doc = CreateDocument();
        const int expectedCredit = 3;
        _repoMock.Setup(r => r.GetByIdAsync(doc.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);
        _providerMock.Setup(p => p.SendAsync(doc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EInvoiceSendResult(true, "REF-CREDIT-001", null, expectedCredit));

        // Act
        await CreateSendHandler().Handle(new SendEInvoiceCommand(doc.Id), CancellationToken.None);

        // Assert
        doc.CreditUsed.Should().Be(expectedCredit);
    }
}
