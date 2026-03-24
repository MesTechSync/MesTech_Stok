using FluentAssertions;
using MesTech.Application.Commands.RejectReturn;

namespace MesTech.Tests.Unit.Application.Validators.Returns;

[Trait("Category", "Unit")]
[Trait("Feature", "Returns")]
public class RejectReturnValidatorTests
{
    private readonly RejectReturnValidator _validator = new();

    [Fact]
    public async Task ValidCommand_Passes()
    {
        var cmd = new RejectReturnCommand(ReturnRequestId: Guid.NewGuid());
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyReturnRequestId_Fails()
    {
        var cmd = new RejectReturnCommand(ReturnRequestId: Guid.Empty);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task RejectionReasonTooLong_Fails()
    {
        var cmd = new RejectReturnCommand(
            ReturnRequestId: Guid.NewGuid(),
            RejectionReason: new string('R', 1001));
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task NullRejectionReason_Passes()
    {
        var cmd = new RejectReturnCommand(
            ReturnRequestId: Guid.NewGuid(),
            RejectionReason: null);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
