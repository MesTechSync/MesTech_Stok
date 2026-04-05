using System.IO;
using FluentAssertions;
using MesTech.Application.Features.Documents.Commands.UploadDocument;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities.Documents;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MesTech.Tests.Unit.Application.Documents.Commands;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class UploadDocumentHandlerTests
{
    private readonly Mock<IDocumentStorageService> _storage = new();
    private readonly Mock<IDocumentRepository> _docRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private UploadDocumentHandler CreateSut() => new(
        _storage.Object, _docRepo.Object, _uow.Object,
        NullLogger<UploadDocumentHandler>.Instance);

    [Fact]
    public async Task Handle_NullRequest_Throws()
    {
        var act = () => CreateSut().Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_ZeroFileSize_ReturnsFailure()
    {
        var cmd = new UploadDocumentCommand(_tenantId, _userId, "test.pdf", "application/pdf",
            0, Stream.Null);

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("boyutu");
    }

    [Fact]
    public async Task Handle_NegativeFileSize_ReturnsFailure()
    {
        var cmd = new UploadDocumentCommand(_tenantId, _userId, "test.pdf", "application/pdf",
            -1, Stream.Null);

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ExceedsMaxSize_ReturnsFailure()
    {
        // 50 MB + 1 byte
        var cmd = new UploadDocumentCommand(_tenantId, _userId, "huge.zip", "application/zip",
            50 * 1024 * 1024 + 1, Stream.Null);

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("50 MB");
    }

    [Fact]
    public async Task Handle_EmptyFileName_ReturnsFailure()
    {
        var cmd = new UploadDocumentCommand(_tenantId, _userId, "", "application/pdf",
            1024, Stream.Null);

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("adi");
    }

    [Fact]
    public async Task Handle_ValidUpload_ReturnsSuccessWithDocumentId()
    {
        _storage.Setup(s => s.UploadAsync(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/documents/test.pdf");

        var cmd = new UploadDocumentCommand(_tenantId, _userId,
            "report.pdf", "application/pdf", 2048, new MemoryStream(new byte[10]));

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.DocumentId.Should().NotBe(Guid.Empty);
        result.StoragePath.Should().Be("/documents/test.pdf");
        _docRepo.Verify(r => r.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_StorageThrows_ReturnsFailure()
    {
        _storage.Setup(s => s.UploadAsync(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("MinIO connection refused"));

        var cmd = new UploadDocumentCommand(_tenantId, _userId,
            "doc.pdf", "application/pdf", 1024, new MemoryStream(new byte[5]));

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("MinIO");
    }

    [Fact]
    public async Task Handle_WithLinkedEntities_SetsLinks()
    {
        var orderId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        _storage.Setup(s => s.UploadAsync(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/docs/linked.pdf");

        var cmd = new UploadDocumentCommand(_tenantId, _userId,
            "invoice.pdf", "application/pdf", 512, new MemoryStream(new byte[3]),
            OrderId: orderId, InvoiceId: invoiceId);

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _docRepo.Verify(r => r.AddAsync(
            It.Is<Document>(d => d.OrderId == orderId && d.InvoiceId == invoiceId),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
