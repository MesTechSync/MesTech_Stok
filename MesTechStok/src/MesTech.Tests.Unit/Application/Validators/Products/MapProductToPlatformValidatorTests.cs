using FluentAssertions;
using MesTech.Application.Commands.MapProductToPlatform;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Products;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class MapProductToPlatformValidatorTests
{
    private readonly MapProductToPlatformValidator _sut = new();

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
    public async Task Platform_WhenInvalidEnum_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Platform = (PlatformType)999 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Platform");
    }

    [Fact]
    public async Task PlatformCategoryId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { PlatformCategoryId = string.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PlatformCategoryId");
    }

    [Fact]
    public async Task PlatformCategoryId_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { PlatformCategoryId = new string('C', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PlatformCategoryId");
    }

    private static MapProductToPlatformCommand CreateValidCommand() => new(
        ProductId: Guid.NewGuid(),
        Platform: PlatformType.Trendyol,
        PlatformCategoryId: "CAT-12345"
    );
}
