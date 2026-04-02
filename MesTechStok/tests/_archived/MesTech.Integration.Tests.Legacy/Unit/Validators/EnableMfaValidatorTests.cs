using FluentAssertions;
using MesTech.Application.Features.Auth.Commands.EnableMfa;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Validators;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class EnableMfaValidatorTests
{
    private readonly EnableMfaValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new EnableMfaCommand(Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_UserId_Fails()
    {
        var cmd = new EnableMfaCommand(Guid.Empty);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }
}
