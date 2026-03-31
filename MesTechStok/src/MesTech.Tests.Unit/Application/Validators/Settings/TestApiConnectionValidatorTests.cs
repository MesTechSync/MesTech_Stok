using FluentAssertions;
using MesTech.Application.Features.Settings.Commands.TestApiConnection;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Settings;

[Trait("Category", "Unit")]
public class TestApiConnectionValidatorTests
{
    private readonly TestApiConnectionValidator _sut = new();

    private static TestApiConnectionCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        ApiBaseUrl: "https://api.mestech.com/v1");

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
    public async Task EmptyApiBaseUrl_ShouldFail()
    {
        var command = CreateValidCommand() with { ApiBaseUrl = "" };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ApiBaseUrl");
    }

    [Fact]
    public async Task ApiBaseUrl_ExceedsMaxLength_ShouldFail()
    {
        var command = CreateValidCommand() with { ApiBaseUrl = "https://" + new string('a', 500) + ".com" };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ApiBaseUrl");
    }

    [Fact]
    public async Task ApiBaseUrl_InvalidUri_ShouldFail()
    {
        var command = CreateValidCommand() with { ApiBaseUrl = "not-a-valid-url" };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ApiBaseUrl");
    }

    [Fact]
    public async Task ApiBaseUrl_HttpScheme_ShouldPass()
    {
        var command = CreateValidCommand() with { ApiBaseUrl = "http://localhost:3100" };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ApiBaseUrl_HttpsScheme_ShouldPass()
    {
        var command = CreateValidCommand() with { ApiBaseUrl = "https://secure.api.com" };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ApiBaseUrl_AtMaxLength_ShouldPass()
    {
        var url = "https://example.com/" + new string('a', 477);
        var command = CreateValidCommand() with { ApiBaseUrl = url };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task BothFields_Empty_ShouldFail_WithMultipleErrors()
    {
        var command = CreateValidCommand() with { TenantId = Guid.Empty, ApiBaseUrl = "" };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task ApiBaseUrl_RelativePath_ShouldFail()
    {
        var command = CreateValidCommand() with { ApiBaseUrl = "/api/v1/health" };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ApiBaseUrl");
    }
}
