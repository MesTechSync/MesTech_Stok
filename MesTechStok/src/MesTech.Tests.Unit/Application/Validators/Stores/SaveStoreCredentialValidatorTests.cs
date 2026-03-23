using FluentAssertions;
using MesTech.Application.Features.Stores.Commands.SaveStoreCredential;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Stores;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class SaveStoreCredentialValidatorTests
{
    private readonly SaveStoreCredentialValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyStoreId_ShouldFail()
    {
        var cmd = CreateValidCommand();
        cmd = cmd with { StoreId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StoreId");
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var cmd = CreateValidCommand();
        cmd = cmd with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptyPlatform_ShouldFail()
    {
        var cmd = CreateValidCommand();
        cmd = cmd with { Platform = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Platform");
    }

    [Fact]
    public async Task PlatformExceeds100Chars_ShouldFail()
    {
        var cmd = CreateValidCommand();
        cmd = cmd with { Platform = new string('P', 101) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Platform");
    }

    [Fact]
    public async Task EmptyCredentialType_ShouldFail()
    {
        var cmd = CreateValidCommand();
        cmd = cmd with { CredentialType = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CredentialType");
    }

    [Fact]
    public async Task InvalidCredentialType_ShouldFail()
    {
        var cmd = CreateValidCommand();
        cmd = cmd with { CredentialType = "basic_auth" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CredentialType");
    }

    [Theory]
    [InlineData("api_key")]
    [InlineData("oauth2")]
    [InlineData("soap")]
    public async Task ValidCredentialType_ShouldPass(string credType)
    {
        var cmd = CreateValidCommand();
        cmd = cmd with { CredentialType = credType };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyFields_ShouldFail()
    {
        var cmd = CreateValidCommand();
        cmd = cmd with { Fields = new Dictionary<string, string>(StringComparer.Ordinal) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Fields");
    }

    [Fact]
    public async Task FieldWithEmptyKey_ShouldFail()
    {
        var cmd = CreateValidCommand();
        cmd = cmd with { Fields = new Dictionary<string, string>(StringComparer.Ordinal) { { "", "value" } } };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task FieldWithEmptyValue_ShouldFail()
    {
        var cmd = CreateValidCommand();
        cmd = cmd with { Fields = new Dictionary<string, string>(StringComparer.Ordinal) { { "ApiKey", "" } } };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    private static SaveStoreCredentialCommand CreateValidCommand() => new()
    {
        StoreId = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        Platform = "Trendyol",
        CredentialType = "api_key",
        Fields = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "ApiKey", "test-api-key-123" },
            { "Secret", "test-secret-456" }
        }
    };
}
