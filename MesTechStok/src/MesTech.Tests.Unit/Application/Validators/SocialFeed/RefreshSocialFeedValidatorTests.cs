using FluentAssertions;
using MesTech.Application.Features.SocialFeed.Commands.RefreshSocialFeed;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.SocialFeed;

[Trait("Category", "Unit")]
public class RefreshSocialFeedValidatorTests
{
    private readonly RefreshSocialFeedValidator _sut = new();

    private static RefreshSocialFeedCommand CreateValidCommand() => new(
        ConfigId: Guid.NewGuid());

    [Fact]
    public async Task ValidCommand_ShouldPassValidation()
    {
        var command = CreateValidCommand();

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyConfigId_ShouldFail()
    {
        var command = CreateValidCommand() with { ConfigId = Guid.Empty };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ConfigId");
    }

    [Fact]
    public async Task EmptyConfigId_ShouldHaveSingleError()
    {
        var command = CreateValidCommand() with { ConfigId = Guid.Empty };

        var result = await _sut.ValidateAsync(command);

        result.Errors.Should().HaveCount(1);
    }

    [Fact]
    public async Task ValidConfigId_ShouldHaveNoErrors()
    {
        var command = CreateValidCommand();

        var result = await _sut.ValidateAsync(command);

        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task DifferentValidGuids_ShouldAllPass()
    {
        var command1 = CreateValidCommand();
        var command2 = CreateValidCommand();

        var result1 = await _sut.ValidateAsync(command1);
        var result2 = await _sut.ValidateAsync(command2);

        result1.IsValid.Should().BeTrue();
        result2.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task SpecificGuid_ShouldPass()
    {
        var command = CreateValidCommand() with
        {
            ConfigId = Guid.Parse("12345678-1234-1234-1234-123456789abc")
        };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyConfigId_ErrorMessage_ShouldNotBeEmpty()
    {
        var command = CreateValidCommand() with { ConfigId = Guid.Empty };

        var result = await _sut.ValidateAsync(command);

        result.Errors.Should().AllSatisfy(e => e.ErrorMessage.Should().NotBeNullOrEmpty());
    }

    [Fact]
    public async Task ValidCommand_Twice_ShouldPassBothTimes()
    {
        var command = CreateValidCommand();

        var result1 = await _sut.ValidateAsync(command);
        var result2 = await _sut.ValidateAsync(command);

        result1.IsValid.Should().BeTrue();
        result2.IsValid.Should().BeTrue();
    }
}
