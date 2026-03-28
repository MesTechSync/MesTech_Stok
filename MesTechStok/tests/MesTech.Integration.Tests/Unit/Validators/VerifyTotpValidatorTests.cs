using FluentAssertions;
using MesTech.Application.Features.Auth.Commands.VerifyTotp;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Validators;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class VerifyTotpValidatorTests
{
    private readonly VerifyTotpValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new VerifyTotpCommand(Guid.NewGuid(), "123456");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_UserId_Fails()
    {
        var cmd = new VerifyTotpCommand(Guid.Empty, "123456");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [Fact]
    public void Empty_Code_Fails()
    {
        var cmd = new VerifyTotpCommand(Guid.NewGuid(), "");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public void Short_Code_Fails()
    {
        var cmd = new VerifyTotpCommand(Guid.NewGuid(), "123");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void NonNumeric_Code_Fails()
    {
        var cmd = new VerifyTotpCommand(Guid.NewGuid(), "abcdef");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void TooLong_Code_Fails()
    {
        var cmd = new VerifyTotpCommand(Guid.NewGuid(), "1234567");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }
}
