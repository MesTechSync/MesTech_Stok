using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Application.Validators.Dropshipping;

[Trait("Category", "Unit")]
[Trait("Feature", "Dropshipping")]
public class CreateFeedSourceValidatorTests
{
    private readonly CreateFeedSourceValidator _validator = new();

    private static CreateFeedSourceCommand ValidCommand() => new(
        SupplierId: Guid.NewGuid(),
        Name: "Supplier XML Feed",
        FeedUrl: "https://supplier.com/feed.xml",
        Format: FeedFormat.Xml,
        PriceMarkupPercent: 15m,
        PriceMarkupFixed: 0m,
        SyncIntervalMinutes: 60,
        TargetPlatforms: "Trendyol,HB",
        AutoDeactivateOnZeroStock: true);

    [Fact]
    public async Task ValidCommand_Passes()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptySupplierId_Fails()
    {
        var cmd = ValidCommand() with { SupplierId = Guid.Empty };
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
    public async Task NameTooLong_Fails()
    {
        var cmd = ValidCommand() with { Name = new string('N', 501) };
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
        var cmd = ValidCommand() with { PriceMarkupPercent = -5m };
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
        var cmd = ValidCommand() with { TargetPlatforms = new string('P', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task NullTargetPlatforms_Passes()
    {
        var cmd = ValidCommand() with { TargetPlatforms = null };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
