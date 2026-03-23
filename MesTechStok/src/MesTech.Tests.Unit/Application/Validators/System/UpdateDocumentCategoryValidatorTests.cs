using FluentAssertions;
using MesTech.Application.Commands.UpdateDocumentCategory;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.System;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class UpdateDocumentCategoryValidatorTests
{
    private readonly UpdateDocumentCategoryValidator _sut = new();

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
    public async Task DocumentType_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { DocumentType = string.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DocumentType");
    }

    [Fact]
    public async Task DocumentType_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { DocumentType = new string('D', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DocumentType");
    }

    [Fact]
    public async Task TenantId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    private static UpdateDocumentCategoryCommand CreateValidCommand() => new()
    {
        DocumentId = Guid.NewGuid(),
        DocumentType = "Invoice",
        Confidence = 0.95m,
        TenantId = Guid.NewGuid()
    };
}
