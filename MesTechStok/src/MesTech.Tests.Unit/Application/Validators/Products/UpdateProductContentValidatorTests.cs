using FluentAssertions;
using MesTech.Application.Commands.UpdateProductContent;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Products;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class UpdateProductContentValidatorTests
{
    private readonly UpdateProductContentValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ProductId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { ProductId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductId");
    }

    [Fact]
    public async Task SKU_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { SKU = string.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SKU");
    }

    [Fact]
    public async Task SKU_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { SKU = new string('S', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SKU");
    }

    [Fact]
    public async Task GeneratedContent_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { GeneratedContent = string.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "GeneratedContent");
    }

    [Fact]
    public async Task GeneratedContent_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { GeneratedContent = new string('G', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "GeneratedContent");
    }

    [Fact]
    public async Task AiProvider_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { AiProvider = string.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AiProvider");
    }

    [Fact]
    public async Task AiProvider_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { AiProvider = new string('A', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AiProvider");
    }

    [Fact]
    public async Task TenantId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    private static UpdateProductContentCommand CreateValidCommand() => new()
    {
        ProductId = Guid.NewGuid(),
        SKU = "SKU-CONTENT-001",
        GeneratedContent = "AI-generated product description",
        AiProvider = "OpenAI-GPT4",
        TenantId = Guid.NewGuid()
    };
}
