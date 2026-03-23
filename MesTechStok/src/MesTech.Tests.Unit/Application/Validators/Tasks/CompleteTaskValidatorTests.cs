using FluentAssertions;
using MesTech.Application.Features.Tasks.Commands.CompleteTask;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Tasks;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CompleteTaskValidatorTests
{
    private readonly CompleteTaskValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new CompleteTaskCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTaskId_ShouldFail()
    {
        var cmd = new CompleteTaskCommand(Guid.Empty, Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TaskId");
    }

    [Fact]
    public async Task EmptyUserId_ShouldFail()
    {
        var cmd = new CompleteTaskCommand(Guid.NewGuid(), Guid.Empty);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }
}
