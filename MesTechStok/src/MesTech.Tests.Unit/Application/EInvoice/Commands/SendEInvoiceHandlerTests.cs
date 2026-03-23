using FluentAssertions;
using MesTech.Application.Features.EInvoice.Commands;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities.EInvoice;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.EInvoice.Commands;

[Trait("Category", "Unit")]
public class SendEInvoiceHandlerTests
{
    private readonly Mock<IEInvoiceDocumentRepository> _repoMock = new();
    private readonly Mock<IEInvoiceProvider> _providerMock = new();
    private readonly Mock<ILogger<SendEInvoiceHandler>> _loggerMock = new();
    private readonly SendEInvoiceHandler _sut;

    public SendEInvoiceHandlerTests()
    {
        _sut = new SendEInvoiceHandler(_repoMock.Object, _providerMock.Object, _loggerMock.Object);
    }

    private static EInvoiceDocument CreateDraftDocument()
    {
        return EInvoiceDocument.Create(
            gibUuid: Guid.NewGuid().ToString(),
            ettnNo: "GGB2026SEND00000001",
            scenario: EInvoiceScenario.TICARIFATURA,
            type: EInvoiceType.SATIS,
            issueDate: DateTime.UtcNow,
            sellerVkn: "0000000000",
            sellerTitle: "MesTech",
            buyerTitle: "Test Alici",
            providerId: "sovos",
            createdBy: "system");
    }

    [Fact]
    public async Task Handle_ProviderSendsSuccessfully_ShouldReturnTrueAndUpdate()
    {
        // Arrange
        var doc = CreateDraftDocument();
        _repoMock.Setup(r => r.GetByIdAsync(doc.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);
        _providerMock.Setup(p => p.SendAsync(doc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EInvoiceSendResult(true, "PROV-REF-001", null, 1));

        // Act
        var result = await _sut.Handle(new SendEInvoiceCommand(doc.Id), CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _repoMock.Verify(r => r.UpdateAsync(doc, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DocumentNotFound_ShouldReturnFalse()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EInvoiceDocument?)null);

        // Act
        var result = await _sut.Handle(new SendEInvoiceCommand(missingId), CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _providerMock.Verify(p => p.SendAsync(It.IsAny<EInvoiceDocument>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ProviderSendFails_ShouldReturnFalseAndNotUpdate()
    {
        // Arrange
        var doc = CreateDraftDocument();
        _repoMock.Setup(r => r.GetByIdAsync(doc.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);
        _providerMock.Setup(p => p.SendAsync(doc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EInvoiceSendResult(false, null, "Provider baglanti hatasi", 0));

        // Act
        var result = await _sut.Handle(new SendEInvoiceCommand(doc.Id), CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<EInvoiceDocument>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var act = () => _sut.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
