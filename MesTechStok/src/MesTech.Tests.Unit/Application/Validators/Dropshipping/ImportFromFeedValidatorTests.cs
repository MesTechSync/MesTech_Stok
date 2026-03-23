using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands.ImportFromFeed;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Dropshipping;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class ImportFromFeedValidatorTests
{
    private readonly ImportFromFeedValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task FeedSourceId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { FeedSourceId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FeedSourceId");
    }

    [Fact]
    public async Task PriceMultiplier_WhenNegative_ShouldFail()
    {
        var cmd = CreateValidCommand() with { PriceMultiplier = -1m };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PriceMultiplier");
    }

    private static ImportFromFeedCommand CreateValidCommand() => new(
        FeedSourceId: Guid.NewGuid(),
        SelectedSkus: new List<string> { "SKU-001", "SKU-002" },
        PriceMultiplier: 1.2m
    );
}
