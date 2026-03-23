using FluentAssertions;
using MesTech.Application.Features.CategoryMapping.Commands.MapCategory;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.CategoryMapping;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class MapCategoryValidatorTests
{
    private readonly MapCategoryValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptyInternalCategoryId_ShouldFail()
    {
        var cmd = CreateValidCommand() with { InternalCategoryId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "InternalCategoryId");
    }

    [Fact]
    public async Task InvalidPlatform_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Platform = (PlatformType)99 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Platform");
    }

    [Fact]
    public async Task EmptyPlatformCategoryId_ShouldFail()
    {
        var cmd = CreateValidCommand() with { PlatformCategoryId = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PlatformCategoryId");
    }

    [Fact]
    public async Task PlatformCategoryIdExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { PlatformCategoryId = new string('1', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PlatformCategoryId");
    }

    [Fact]
    public async Task EmptyPlatformCategoryName_ShouldFail()
    {
        var cmd = CreateValidCommand() with { PlatformCategoryName = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PlatformCategoryName");
    }

    [Fact]
    public async Task PlatformCategoryNameExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { PlatformCategoryName = new string('C', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PlatformCategoryName");
    }

    private static MapCategoryCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        InternalCategoryId: Guid.NewGuid(),
        Platform: PlatformType.Trendyol,
        PlatformCategoryId: "12345",
        PlatformCategoryName: "Elektronik > Bilgisayar"
    );
}
