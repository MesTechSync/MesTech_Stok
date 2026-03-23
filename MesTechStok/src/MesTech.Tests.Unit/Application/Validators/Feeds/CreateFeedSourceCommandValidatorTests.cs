using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Application.Validators.Feeds;

[Trait("Category", "Unit")]
[Trait("Feature", "Feeds")]
public class CreateFeedSourceCommandValidatorTests
{
    private readonly CreateFeedSourceCommandValidator _validator = new();

    private static CreateFeedSourceCommand CreateValidCommand() => new(
        SupplierId: Guid.NewGuid(),
        Name: "Test Feed",
        FeedUrl: "https://supplier.com/feed.xml",
        Format: FeedFormat.Xml,
        PriceMarkupPercent: 10m,
        PriceMarkupFixed: 0m,
        SyncIntervalMinutes: 60,
        TargetPlatforms: "Trendyol,Hepsiburada",
        AutoDeactivateOnZeroStock: true
    );

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var result = await _validator.ValidateAsync(CreateValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task SupplierId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { SupplierId = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SupplierId");
    }

    [Fact]
    public async Task Name_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Name = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Name_WhenTooLong_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Name = new string('A', 201) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Name_WhenExactly200Chars_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Name = new string('A', 200) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task FeedUrl_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { FeedUrl = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FeedUrl");
    }

    [Fact]
    public async Task FeedUrl_WhenInvalidUrl_ShouldFail()
    {
        var cmd = CreateValidCommand() with { FeedUrl = "not-a-url" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FeedUrl");
    }

    [Fact]
    public async Task FeedUrl_WhenValidHttpsUrl_ShouldPass()
    {
        var cmd = CreateValidCommand() with { FeedUrl = "https://example.com/feed.xml" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Format_WhenInvalidEnum_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Format = (FeedFormat)999 };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Format");
    }

    [Fact]
    public async Task PriceMarkupPercent_WhenNegative_ShouldFail()
    {
        var cmd = CreateValidCommand() with { PriceMarkupPercent = -1m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PriceMarkupPercent");
    }

    [Fact]
    public async Task PriceMarkupPercent_WhenExceeds500_ShouldFail()
    {
        var cmd = CreateValidCommand() with { PriceMarkupPercent = 501m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PriceMarkupPercent");
    }

    [Fact]
    public async Task PriceMarkupPercent_WhenZero_ShouldPass()
    {
        var cmd = CreateValidCommand() with { PriceMarkupPercent = 0m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task SyncIntervalMinutes_WhenBelowMinimum_ShouldFail()
    {
        var cmd = CreateValidCommand() with { SyncIntervalMinutes = 4 };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SyncIntervalMinutes");
    }

    [Fact]
    public async Task SyncIntervalMinutes_WhenAboveMaximum_ShouldFail()
    {
        var cmd = CreateValidCommand() with { SyncIntervalMinutes = 1441 };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SyncIntervalMinutes");
    }

    [Theory]
    [InlineData(5)]
    [InlineData(60)]
    [InlineData(1440)]
    public async Task SyncIntervalMinutes_WhenInRange_ShouldPass(int interval)
    {
        var cmd = CreateValidCommand() with { SyncIntervalMinutes = interval };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
