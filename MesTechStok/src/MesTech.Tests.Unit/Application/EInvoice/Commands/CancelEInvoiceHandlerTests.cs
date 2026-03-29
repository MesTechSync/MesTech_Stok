using FluentAssertions;
using MesTech.Application.Features.EInvoice.Commands;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities.EInvoice;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.EInvoice.Commands;

[Trait("Category", "Unit")]
public class CancelEInvoiceHandlerTests
{
    private readonly Mock<IEInvoiceDocumentRepository> _repoMock = new();
    private readonly Mock<IEInvoiceProvider> _providerMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CancelEInvoiceHandler _sut;

    public CancelEInvoiceHandlerTests()
    {
        _sut = new CancelEInvoiceHandler(_repoMock.Object, _providerMock.Object, _uowMock.Object);
    }

    private static EInvoiceDocument CreateDraftDocument()
    {
        return EInvoiceDocument.Create(
            gibUuid: Guid.NewGuid().ToString(),
            ettnNo: "GGB2026TEST00000001",
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
    public async Task Handle_DraftDocumentNoProviderRef_ShouldCancelLocally()
    {
        // Arrange
        var doc = CreateDraftDocument();
        _repoMock.Setup(r => r.GetByIdAsync(doc.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);

        // Act
        var result = await _sut.Handle(
            new CancelEInvoiceCommand(doc.Id, "Test cancel"), CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _repoMock.Verify(r => r.UpdateAsync(doc, It.IsAny<CancellationToken>()), Times.Once);
        _providerMock.Verify(p => p.CancelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DocumentNotFound_ShouldReturnFalse()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EInvoiceDocument?)null);

        // Act
        var result = await _sut.Handle(
            new CancelEInvoiceCommand(missingId, "Reason"), CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var act = () => _sut.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_SentDocWithProviderRef_ProviderCancelFails_ShouldReturnFalse()
    {
        // Arrange
        var doc = CreateDraftDocument();
        doc.MarkAsSent("PROV-REF-123", creditUsed: 1);
        _repoMock.Setup(r => r.GetByIdAsync(doc.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);
        _providerMock.Setup(p => p.CancelAsync("PROV-REF-123", "Reason", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.Handle(
            new CancelEInvoiceCommand(doc.Id, "Reason"), CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<EInvoiceDocument>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
