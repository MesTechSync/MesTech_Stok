using FluentAssertions;
using MesTech.Application.Commands.GenerateEFatura;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.EInvoice;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GenerateEFaturaValidatorTests
{
    private readonly GenerateEFaturaValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task BotUserId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { BotUserId = string.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BotUserId");
    }

    [Fact]
    public async Task BotUserId_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { BotUserId = new string('B', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BotUserId");
    }

    [Fact]
    public async Task TenantId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    private static GenerateEFaturaCommand CreateValidCommand() => new()
    {
        BotUserId = "bot-user-001",
        OrderId = Guid.NewGuid(),
        BuyerVkn = "1234567890",
        TenantId = Guid.NewGuid()
    };
}
