using FluentAssertions;
using MesTech.Application.Commands.GenerateEFatura;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class GenerateEFaturaValidatorTests
{
    private readonly GenerateEFaturaValidator _validator = new();

    private static GenerateEFaturaCommand ValidCommand() => new()
    {
        BotUserId = "bot-001",
        OrderId = Guid.NewGuid(),
        BuyerVkn = "1234567890",
        TenantId = Guid.NewGuid()
    };

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_BotUserId_Fails()
    {
        var cmd = ValidCommand();
        cmd = cmd with { BotUserId = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BotUserId");
    }

    [Fact]
    public void BotUserId_Over500_Fails()
    {
        var cmd = ValidCommand() with { BotUserId = new string('B', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BotUserId");
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = ValidCommand() with { TenantId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void Null_OrderId_Passes()
    {
        var cmd = ValidCommand() with { OrderId = null };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "OrderId");
    }
}
