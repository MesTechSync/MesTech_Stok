using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands;

namespace MesTech.Tests.Unit.Application.Validators.Feeds;

[Trait("Category", "Unit")]
[Trait("Feature", "Feeds")]
public class DeleteFeedSourceValidatorTests
{
    private readonly DeleteFeedSourceValidator _validator = new();

    private static DeleteFeedSourceCommand CreateValidCommand() => new(
        Id: Guid.NewGuid()
    );

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var result = await _validator.ValidateAsync(CreateValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Id_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Id = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }
}
