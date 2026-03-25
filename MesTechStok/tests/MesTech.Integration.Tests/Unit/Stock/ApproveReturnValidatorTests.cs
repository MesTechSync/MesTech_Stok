using FluentAssertions;
using MesTech.Application.Commands.ApproveReturn;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class ApproveReturnValidatorTests
{
    private readonly ApproveReturnValidator _validator = new();

    private static ApproveReturnCommand ValidCommand() => new(
        ReturnRequestId: Guid.NewGuid(),
        AutoRestoreStock: true);

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_ReturnRequestId_Fails()
    {
        var cmd = ValidCommand() with { ReturnRequestId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReturnRequestId");
    }

    [Fact]
    public void AutoRestoreStock_False_Passes()
    {
        var cmd = ValidCommand() with { AutoRestoreStock = false };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}
