using FluentAssertions;
using MesTech.Application.Features.Hr.Commands.ApproveLeave;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Hr;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class ApproveLeaveValidatorTests
{
    private readonly ApproveLeaveValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new ApproveLeaveCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyLeaveId_ShouldFail()
    {
        var cmd = new ApproveLeaveCommand(Guid.Empty, Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LeaveId");
    }

    [Fact]
    public async Task EmptyApproverUserId_ShouldFail()
    {
        var cmd = new ApproveLeaveCommand(Guid.NewGuid(), Guid.Empty);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ApproverUserId");
    }
}
