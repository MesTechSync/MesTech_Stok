using System.IO;
using FluentAssertions;
using MesTech.Application.Features.Product.Commands.ExecuteBulkImport;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// DEV5 (D5-113): ExecuteBulkImportHandler unit tests.
/// Covers null guard, empty stream, extension validation, option passthrough, and service delegation.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "BulkImport")]
public class ExecuteBulkImportHandlerTests
{
    private readonly Mock<IBulkProductImportService> _importServiceMock = new();

    private ExecuteBulkImportHandler CreateSut() => new(_importServiceMock.Object);

    // ── Guard: null request ──

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        var act = () => sut.Handle(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ── Guard: null / empty stream ──

    [Fact]
    public async Task Handle_NullFileStream_ReturnsFailedWithFileError()
    {
        var cmd = new ExecuteBulkImportCommand(null!, "products.xlsx");
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.Status.Should().Be(ImportStatus.Failed);
        result.ErrorCount.Should().Be(1);
        result.Errors.Should().ContainSingle(e => e.Field == "File");
        result.Errors[0].Message.Should().Contain("boş");
    }

    [Fact]
    public async Task Handle_EmptyStream_ReturnsFailedWithFileError()
    {
        using var emptyStream = new MemoryStream();
        var cmd = new ExecuteBulkImportCommand(emptyStream, "products.xlsx");

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.Status.Should().Be(ImportStatus.Failed);
        result.ErrorCount.Should().Be(1);
        result.TotalRows.Should().Be(0);
        result.Duration.Should().Be(TimeSpan.Zero);
    }

    // ── Guard: file extension ──

    [Theory]
    [InlineData("products.csv")]
    [InlineData("products.txt")]
    [InlineData("products.json")]
    [InlineData("products.pdf")]
    [InlineData("products")]
    public async Task Handle_UnsupportedExtension_ReturnsFailedWithExtensionError(string fileName)
    {
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var cmd = new ExecuteBulkImportCommand(stream, fileName);

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.Status.Should().Be(ImportStatus.Failed);
        result.ErrorCount.Should().Be(1);
        result.Errors.Should().ContainSingle(e => e.Message.Contains(".xlsx"));
    }

    [Theory]
    [InlineData("products.xlsx")]
    [InlineData("PRODUCTS.XLSX")]
    [InlineData("products.xls")]
    [InlineData("PRODUCTS.XLS")]
    public async Task Handle_SupportedExtension_DelegatesToService(string fileName)
    {
        var expected = new ImportResult(
            ImportStatus.Completed, 50, 50, 0, 0, 0, new List<ImportRowError>(), TimeSpan.FromSeconds(2));
        _importServiceMock
            .Setup(s => s.ImportProductsAsync(It.IsAny<Stream>(), It.IsAny<ImportOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var cmd = new ExecuteBulkImportCommand(stream, fileName);

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.Should().BeSameAs(expected);
        _importServiceMock.Verify(
            s => s.ImportProductsAsync(It.IsAny<Stream>(), It.IsAny<ImportOptions>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Options passthrough ──

    [Fact]
    public async Task Handle_PassesOptionsCorrectly_UpdateExistingAndPlatform()
    {
        ImportOptions? capturedOptions = null;
        _importServiceMock
            .Setup(s => s.ImportProductsAsync(It.IsAny<Stream>(), It.IsAny<ImportOptions>(), It.IsAny<CancellationToken>()))
            .Callback<Stream, ImportOptions, CancellationToken>((_, opts, _) => capturedOptions = opts)
            .ReturnsAsync(new ImportResult(
                ImportStatus.Completed, 10, 10, 0, 0, 0, new List<ImportRowError>(), TimeSpan.FromSeconds(1)));

        var categoryId = Guid.NewGuid();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var cmd = new ExecuteBulkImportCommand(
            stream,
            "products.xlsx",
            UpdateExisting: true,
            SkipErrors: false,
            TargetPlatform: PlatformType.Trendyol,
            CategoryId: categoryId);

        await CreateSut().Handle(cmd, CancellationToken.None);

        capturedOptions.Should().NotBeNull();
        capturedOptions!.UpdateExisting.Should().BeTrue();
        capturedOptions.SkipErrors.Should().BeFalse();
        capturedOptions.TargetPlatform.Should().Be(PlatformType.Trendyol);
        capturedOptions.CategoryId.Should().Be(categoryId);
    }

    [Fact]
    public async Task Handle_DefaultOptions_PassesCorrectDefaults()
    {
        ImportOptions? capturedOptions = null;
        _importServiceMock
            .Setup(s => s.ImportProductsAsync(It.IsAny<Stream>(), It.IsAny<ImportOptions>(), It.IsAny<CancellationToken>()))
            .Callback<Stream, ImportOptions, CancellationToken>((_, opts, _) => capturedOptions = opts)
            .ReturnsAsync(new ImportResult(
                ImportStatus.Completed, 5, 5, 0, 0, 0, new List<ImportRowError>(), TimeSpan.FromMilliseconds(500)));

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var cmd = new ExecuteBulkImportCommand(stream, "data.xlsx");

        await CreateSut().Handle(cmd, CancellationToken.None);

        capturedOptions.Should().NotBeNull();
        capturedOptions!.UpdateExisting.Should().BeFalse();
        capturedOptions.SkipErrors.Should().BeTrue();
        capturedOptions.TargetPlatform.Should().BeNull();
        capturedOptions.CategoryId.Should().BeNull();
    }

    // ── Cancellation token forwarding ──

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToService()
    {
        using var cts = new CancellationTokenSource();
        CancellationToken capturedToken = default;
        _importServiceMock
            .Setup(s => s.ImportProductsAsync(It.IsAny<Stream>(), It.IsAny<ImportOptions>(), It.IsAny<CancellationToken>()))
            .Callback<Stream, ImportOptions, CancellationToken>((_, _, ct) => capturedToken = ct)
            .ReturnsAsync(new ImportResult(
                ImportStatus.Completed, 1, 1, 0, 0, 0, new List<ImportRowError>(), TimeSpan.Zero));

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var cmd = new ExecuteBulkImportCommand(stream, "test.xlsx");

        await CreateSut().Handle(cmd, cts.Token);

        capturedToken.Should().Be(cts.Token);
    }

    // ── Service returns partial errors ──

    [Fact]
    public async Task Handle_ServiceReturnsCompletedWithErrors_ResultIsPreserved()
    {
        var errors = new List<ImportRowError>
        {
            new(5, "Price", "Fiyat negatif olamaz."),
            new(12, "SKU", "SKU zaten mevcut.")
        };
        var expected = new ImportResult(
            ImportStatus.CompletedWithErrors, 100, 95, 3, 0, 2, errors, TimeSpan.FromSeconds(5));
        _importServiceMock
            .Setup(s => s.ImportProductsAsync(It.IsAny<Stream>(), It.IsAny<ImportOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var cmd = new ExecuteBulkImportCommand(stream, "mixed.xlsx");

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.Status.Should().Be(ImportStatus.CompletedWithErrors);
        result.ImportedCount.Should().Be(95);
        result.ErrorCount.Should().Be(2);
        result.Errors.Should().HaveCount(2);
    }

    // ── Service does NOT get called for invalid input ──

    [Fact]
    public async Task Handle_InvalidInput_DoesNotCallService()
    {
        using var emptyStream = new MemoryStream();
        var cmd = new ExecuteBulkImportCommand(emptyStream, "products.xlsx");

        await CreateSut().Handle(cmd, CancellationToken.None);

        _importServiceMock.Verify(
            s => s.ImportProductsAsync(It.IsAny<Stream>(), It.IsAny<ImportOptions>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WrongExtension_DoesNotCallService()
    {
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var cmd = new ExecuteBulkImportCommand(stream, "products.csv");

        await CreateSut().Handle(cmd, CancellationToken.None);

        _importServiceMock.Verify(
            s => s.ImportProductsAsync(It.IsAny<Stream>(), It.IsAny<ImportOptions>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
