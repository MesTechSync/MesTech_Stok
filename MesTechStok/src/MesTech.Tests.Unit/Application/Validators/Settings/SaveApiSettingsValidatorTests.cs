using FluentAssertions;
using MesTech.Application.Features.Settings.Commands.SaveApiSettings;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Settings;

[Trait("Category", "Unit")]
public class SaveApiSettingsValidatorTests
{
    private readonly SaveApiSettingsValidator _sut = new();

    private static SaveApiSettingsCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        ApiBaseUrl: "https://api.mestech.com",
        WebhookSecret: "wh_secret_abc123",
        RateLimitPerMinute: 60,
        EnableCors: true);

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
        var command = CreateValidCommand() with { ApiBaseUrl = "not-a-valid-uri" };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ApiBaseUrl");
    }

    [Fact]
    public async Task RateLimitPerMinute_AtMinimum_ShouldPass()
    {
        var command = CreateValidCommand() with { RateLimitPerMinute = 1 };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RateLimitPerMinute_AtMaximum_ShouldPass()
    {
        var command = CreateValidCommand() with { RateLimitPerMinute = 10000 };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RateLimitPerMinute_BelowMinimum_ShouldFail()
    {
        var command = CreateValidCommand() with { RateLimitPerMinute = 0 };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RateLimitPerMinute");
    }

    [Fact]
    public async Task RateLimitPerMinute_AboveMaximum_ShouldFail()
    {
        var command = CreateValidCommand() with { RateLimitPerMinute = 10001 };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RateLimitPerMinute");
    }

    [Fact]
    public async Task NullWebhookSecret_ShouldPass()
    {
        var command = CreateValidCommand() with { WebhookSecret = null };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task WebhookSecret_ExceedsMaxLength_ShouldFail()
    {
        var command = CreateValidCommand() with { WebhookSecret = new string('x', 257) };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "WebhookSecret");
    }

    [Fact]
    public async Task EnableCors_False_ShouldPass()
    {
        var command = CreateValidCommand() with { EnableCors = false };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task WebhookSecret_AtMaxLength_ShouldPass()
    {
        var command = CreateValidCommand() with { WebhookSecret = new string('s', 256) };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
