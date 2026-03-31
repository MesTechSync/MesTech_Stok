using System.IO;
using FluentAssertions;
using MesTech.Application.Features.Documents.Commands.UploadDocument;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Documents;

[Trait("Category", "Unit")]
public class UploadDocumentValidatorTests
{
    private readonly UploadDocumentValidator _sut = new();

    private static UploadDocumentCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        UserId: Guid.NewGuid(),
        FileName: "report-2026.pdf",
        ContentType: "application/pdf",
        FileSizeBytes: 1024 * 1024,
        FileStream: new MemoryStream(new byte[] { 0x01 }),
        FolderId: Guid.NewGuid());

    [Fact]
    public async Task ValidCommand_ShouldPassValidation()
    {
        var command = CreateValidCommand();

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var command = CreateValidCommand() with { TenantId = Guid.Empty };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptyUserId_ShouldFail()
    {
        var command = CreateValidCommand() with { UserId = Guid.Empty };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [Fact]
    public async Task EmptyFileName_ShouldFail()
    {
        var command = CreateValidCommand() with { FileName = "" };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileName");
    }

    [Fact]
    public async Task FileName_ExceedsMaxLength_ShouldFail()
    {
        var command = CreateValidCommand() with { FileName = new string('f', 256) };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileName");
    }

    [Fact]
    public async Task EmptyContentType_ShouldFail()
    {
        var command = CreateValidCommand() with { ContentType = "" };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ContentType");
    }

    [Fact]
    public async Task InvalidContentType_ShouldFail()
    {
        var command = CreateValidCommand() with { ContentType = "application/zip" };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ContentType");
    }

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/gif")]
    [InlineData("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("text/csv")]
    [InlineData("text/plain")]
    [InlineData("application/xml")]
    [InlineData("application/json")]
    public async Task AllowedContentTypes_ShouldPass(string contentType)
    {
        var command = CreateValidCommand() with { ContentType = contentType };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ZeroFileSizeBytes_ShouldFail()
    {
        var command = CreateValidCommand() with { FileSizeBytes = 0 };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileSizeBytes");
    }

    [Fact]
    public async Task NegativeFileSizeBytes_ShouldFail()
    {
        var command = CreateValidCommand() with { FileSizeBytes = -1 };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileSizeBytes");
    }

    [Fact]
    public async Task NullFileStream_ShouldFail()
    {
        var command = CreateValidCommand() with { FileStream = null! };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileStream");
    }

    [Fact]
    public async Task NullFolderId_ShouldPass()
    {
        var command = CreateValidCommand() with { FolderId = null };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task FileName_AtMaxLength_ShouldPass()
    {
        var command = CreateValidCommand() with { FileName = new string('f', 251) + ".pdf" };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task FileSizeBytes_OneByteMin_ShouldPass()
    {
        var command = CreateValidCommand() with { FileSizeBytes = 1 };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
