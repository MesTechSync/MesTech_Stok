using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.UploadAccountingDocument;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Commands;

[Trait("Category", "Unit")]
public class UploadAccountingDocumentHandlerTests
{
    private readonly Mock<IAccountingDocumentRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly UploadAccountingDocumentHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public UploadAccountingDocumentHandlerTests()
    {
        _sut = new UploadAccountingDocumentHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidInvoiceDocument_CreatesAndReturnsId()
    {
        // Arrange
        var command = new UploadAccountingDocumentCommand(
            TenantId,
            "fatura-001.pdf",
            "application/pdf",
            1_048_576L,
            "/docs/2026/03/fatura-001.pdf",
            DocumentType.Invoice,
            DocumentSource.Upload,
            CounterpartyId: Guid.NewGuid(),
            Amount: 5_000m,
            ExtractedData: "{\"vendor\":\"ABC Ltd.\"}");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _repoMock.Verify(r => r.AddAsync(
            It.IsAny<AccountingDocument>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_DocumentWithoutOptionalFields_CreatesSuccessfully()
    {
        // Arrange
        var command = new UploadAccountingDocumentCommand(
            TenantId,
            "dekont.jpg",
            "image/jpeg",
            512_000L,
            "/docs/2026/03/dekont.jpg",
            DocumentType.Receipt,
            DocumentSource.WhatsApp);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_BankStatementFromEmail_CreatesSuccessfully()
    {
        // Arrange
        var command = new UploadAccountingDocumentCommand(
            TenantId,
            "ekstre-mart.xml",
            "application/xml",
            256_000L,
            "/docs/2026/03/ekstre-mart.xml",
            DocumentType.BankStatement,
            DocumentSource.Email,
            Amount: 125_000m);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
    }
}
