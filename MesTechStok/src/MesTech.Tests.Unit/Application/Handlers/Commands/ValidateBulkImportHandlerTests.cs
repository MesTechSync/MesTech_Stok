using System.IO;
using FluentAssertions;
using MesTech.Application.Features.Product.Commands.ValidateBulkImport;
using MesTech.Application.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

/// <summary>
/// DEV5: ValidateBulkImportHandler testi — Excel import doğrulama.
/// P1: Yanlış import = stok verisi bozulur.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class ValidateBulkImportHandlerTests
{
    private readonly Mock<IBulkProductImportService> _importService = new();

    private ValidateBulkImportHandler CreateSut() => new(_importService.Object);

    [Fact]
    public async Task Handle_NullStream_ShouldReturnInvalid()
    {
        var cmd = new ValidateBulkImportCommand(null!, "test.xlsx");
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("boş"));
    }

    [Fact]
    public async Task Handle_EmptyStream_ShouldReturnInvalid()
    {
        using var emptyStream = new MemoryStream();
        var cmd = new ValidateBulkImportCommand(emptyStream, "test.xlsx");
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_InvalidExtension_ShouldReturnInvalid()
    {
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var cmd = new ValidateBulkImportCommand(stream, "test.csv");
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Message.Contains(".xlsx"));
    }

    [Fact]
    public async Task Handle_ValidXlsx_ShouldDelegateToService()
    {
        var expected = new ImportValidationResult(true, 100, 100, 0, new());
        _importService.Setup(s => s.ValidateExcelAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var cmd = new ValidateBulkImportCommand(stream, "products.xlsx");
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task Handle_XlsExtension_ShouldAlsoBeAccepted()
    {
        var expected = new ImportValidationResult(true, 50, 50, 0, new());
        _importService.Setup(s => s.ValidateExcelAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var cmd = new ValidateBulkImportCommand(stream, "products.xls");
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }
}
