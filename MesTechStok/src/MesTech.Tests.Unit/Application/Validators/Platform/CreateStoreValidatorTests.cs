using FluentAssertions;
using MesTech.Application.Features.Platform.Commands.CreateStore;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Platform;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CreateStoreValidatorTests
{
    private readonly CreateStoreValidator _sut = new();

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
    public async Task EmptyStoreName_ShouldFail()
    {
        var cmd = CreateValidCommand() with { StoreName = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StoreName");
    }

    [Fact]
    public async Task StoreNameExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { StoreName = new string('S', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StoreName");
    }

    [Fact]
    public async Task InvalidPlatformType_ShouldFail()
    {
        var cmd = CreateValidCommand() with { PlatformType = (PlatformType)99 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PlatformType");
    }

    private static CreateStoreCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        StoreName: "Trendyol Magaza",
        PlatformType: PlatformType.Trendyol,
        Credentials: new Dictionary<string, string> { { "ApiKey", "test" } }
    );
}
