using FluentAssertions;
using MesTech.Application.Commands.UpdateDocumentMetadata;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.System;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class UpdateDocumentMetadataValidatorTests
{
    private readonly UpdateDocumentMetadataValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task DocumentId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { DocumentId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DocumentId");
    }

    [Fact]
    public async Task ProcessedJson_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { ProcessedJson = string.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProcessedJson");
    }

    [Fact]
    public async Task ProcessedJson_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { ProcessedJson = new string('J', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProcessedJson");
    }

    [Fact]
    public async Task TenantId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    private static UpdateDocumentMetadataCommand CreateValidCommand() => new()
    {
        DocumentId = Guid.NewGuid(),
        ProcessedJson = "{\"type\":\"invoice\",\"amount\":100}",
        Confidence = 0.90m,
        TenantId = Guid.NewGuid()
    };
}
