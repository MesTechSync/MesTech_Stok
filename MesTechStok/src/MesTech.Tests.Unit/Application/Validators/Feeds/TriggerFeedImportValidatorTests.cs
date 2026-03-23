using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands;

namespace MesTech.Tests.Unit.Application.Validators.Feeds;

[Trait("Category", "Unit")]
[Trait("Feature", "Feeds")]
public class TriggerFeedImportValidatorTests
{
    private readonly TriggerFeedImportValidator _validator = new();

    private static TriggerFeedImportCommand CreateValidCommand() => new(
        FeedId: Guid.NewGuid()
    );

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var result = await _validator.ValidateAsync(CreateValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task FeedId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { FeedId = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FeedId");
    }
}
