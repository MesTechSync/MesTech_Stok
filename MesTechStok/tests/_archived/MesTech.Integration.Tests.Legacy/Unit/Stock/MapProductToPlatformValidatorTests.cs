using FluentAssertions;
using MesTech.Application.Commands.MapProductToPlatform;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class MapProductToPlatformValidatorTests
{
    private readonly MapProductToPlatformValidator _validator = new();

    private static MapProductToPlatformCommand ValidCommand() => new(
        ProductId: Guid.NewGuid(),
        Platform: PlatformType.Trendyol,
        PlatformCategoryId: "12345");

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_ProductId_Fails()
    {
        var cmd = ValidCommand() with { ProductId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductId");
    }

    [Fact]
    public void Invalid_Platform_Fails()
    {
        var cmd = ValidCommand() with { Platform = (PlatformType)999 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Platform");
    }

    [Fact]
    public void Empty_PlatformCategoryId_Fails()
    {
        var cmd = ValidCommand() with { PlatformCategoryId = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PlatformCategoryId");
    }

    [Fact]
    public void PlatformCategoryId_Over500_Fails()
    {
        var cmd = ValidCommand() with { PlatformCategoryId = new string('P', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PlatformCategoryId");
    }
}
