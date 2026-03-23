using FluentAssertions;
using MesTech.Application.Commands.UpdateProductImage;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Products;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class UpdateProductImageValidatorTests
{
    private readonly UpdateProductImageValidator _sut = new();

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
    public async Task ImageUrl_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { ImageUrl = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ImageUrl");
    }

    [Fact]
    public async Task ImageUrl_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { ImageUrl = new string('u', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ImageUrl");
    }

    [Fact]
    public async Task ImageUrl_WhenExactly500Chars_ShouldPass()
    {
        var cmd = CreateValidCommand() with { ImageUrl = new string('u', 500) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    private static UpdateProductImageCommand CreateValidCommand() => new(
        ProductId: Guid.NewGuid(),
        ImageUrl: "https://cdn.mestech.com/products/img-001.jpg"
    );
}
