using FluentAssertions;
using MesTech.Application.Commands.ApproveReturn;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Returns;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class ApproveReturnCommandValidatorTests
{
    private readonly ApproveReturnCommandValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new ApproveReturnCommand(ReturnRequestId: Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ReturnRequestId_WhenEmpty_ShouldFail()
    {
        var cmd = new ApproveReturnCommand(ReturnRequestId: Guid.Empty);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReturnRequestId");
    }
}
