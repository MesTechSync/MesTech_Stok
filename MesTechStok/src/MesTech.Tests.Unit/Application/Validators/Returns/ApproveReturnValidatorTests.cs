using FluentAssertions;
using MesTech.Application.Commands.ApproveReturn;

namespace MesTech.Tests.Unit.Application.Validators.Returns;

[Trait("Category", "Unit")]
[Trait("Feature", "Returns")]
public class ApproveReturnValidatorTests
{
    private readonly ApproveReturnValidator _validator = new();

    [Fact]
    public async Task ValidCommand_Passes()
    {
        var cmd = new ApproveReturnCommand(ReturnRequestId: Guid.NewGuid());
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyReturnRequestId_Fails()
    {
        var cmd = new ApproveReturnCommand(ReturnRequestId: Guid.Empty);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }
}
