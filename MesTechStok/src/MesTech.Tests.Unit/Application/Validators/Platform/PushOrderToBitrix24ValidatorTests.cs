using FluentAssertions;
using MesTech.Application.Commands.PushOrderToBitrix24;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Platform;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class PushOrderToBitrix24ValidatorTests
{
    private readonly PushOrderToBitrix24Validator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task OrderId_WhenEmpty_ShouldFail()
    {
        var cmd = new PushOrderToBitrix24Command(Guid.Empty);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderId");
    }

    private static PushOrderToBitrix24Command CreateValidCommand() => new(Guid.NewGuid());
}
