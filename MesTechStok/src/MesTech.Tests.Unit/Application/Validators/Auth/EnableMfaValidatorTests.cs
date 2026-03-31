using FluentAssertions;
using MesTech.Application.Features.Auth.Commands.EnableMfa;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Auth;

[Trait("Category", "Unit")]
public class EnableMfaValidatorTests
{
    private readonly EnableMfaValidator _sut = new();

    private static EnableMfaCommand CreateValidCommand() =>
        new(Guid.NewGuid());

    [Fact]
    public async Task ValidCommand_ShouldPassValidation()
    {
        var command = CreateValidCommand();
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task UserId_WhenEmpty_ShouldFail()
    {
        var command = CreateValidCommand() with { UserId = Guid.Empty };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [Fact]
    public async Task UserId_WhenValidGuid_ShouldPass()
    {
        var command = CreateValidCommand() with { UserId = Guid.Parse("11111111-1111-1111-1111-111111111111") };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task UserId_WhenNewGuid_ShouldPass()
    {
        var command = new EnableMfaCommand(Guid.NewGuid());
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task UserId_WhenEmptyGuid_ErrorMessageShouldBeRelevant()
    {
        var command = CreateValidCommand() with { UserId = Guid.Empty };
        var result = await _sut.ValidateAsync(command);
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be("UserId");
    }

    [Fact]
    public async Task UserId_WhenDefault_ShouldFail()
    {
        var command = CreateValidCommand() with { UserId = default };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task UserId_WhenMultipleNewGuids_AllShouldPass()
    {
        for (int i = 0; i < 5; i++)
        {
            var command = new EnableMfaCommand(Guid.NewGuid());
            var result = await _sut.ValidateAsync(command);
            result.IsValid.Should().BeTrue();
        }
    }

    [Fact]
    public async Task Validator_ShouldHaveExactlyOneRuleForUserId()
    {
        var command = CreateValidCommand() with { UserId = Guid.Empty };
        var result = await _sut.ValidateAsync(command);
        result.Errors.Where(e => e.PropertyName == "UserId").Should().HaveCount(1);
    }
}
