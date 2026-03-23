using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands.PreviewFeed;

namespace MesTech.Tests.Unit.Application.Validators.Feeds;

[Trait("Category", "Unit")]
[Trait("Feature", "Feeds")]
public class PreviewFeedValidatorTests
{
    private readonly PreviewFeedValidator _validator = new();

    private static PreviewFeedCommand CreateValidCommand() => new(
        FeedSourceId: Guid.NewGuid()
    );

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var result = await _validator.ValidateAsync(CreateValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task FeedSourceId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { FeedSourceId = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FeedSourceId");
    }
}
