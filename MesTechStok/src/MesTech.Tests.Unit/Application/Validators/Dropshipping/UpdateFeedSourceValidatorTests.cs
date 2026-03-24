using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Application.Validators.Dropshipping;

[Trait("Category", "Unit")]
[Trait("Feature", "Dropshipping")]
public class UpdateFeedSourceValidatorTests
{
    private readonly UpdateFeedSourceValidator _validator = new();

    private static UpdateFeedSourceCommand ValidCommand() => new(
        Id: Guid.NewGuid(),
        Name: "Updated Feed",
        FeedUrl: "https://supplier.com/feed.xml",
        Format: FeedFormat.Xml,
        PriceMarkupPercent: 10m,
        PriceMarkupFixed: 0m,
        SyncIntervalMinutes: 60,
        TargetPlatforms: "Trendyol",
        AutoDeactivateOnZeroStock: true,
        IsActive: true);

    [Fact]
    public async Task ValidCommand_Passes()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyId_Fails()
    {
        var cmd = ValidCommand() with { Id = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task EmptyName_Fails()
    {
        var cmd = ValidCommand() with { Name = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task EmptyFeedUrl_Fails()
    {
        var cmd = ValidCommand() with { FeedUrl = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task NegativeMarkupPercent_Fails()
    {
        var cmd = ValidCommand() with { PriceMarkupPercent = -1m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task NegativeMarkupFixed_Fails()
    {
        var cmd = ValidCommand() with { PriceMarkupFixed = -1m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task TargetPlatformsTooLong_Fails()
    {
        var cmd = ValidCommand() with { TargetPlatforms = new string('X', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }
}
