using FluentAssertions;
using MesTech.Application.Features.Settings.Commands.SaveFulfillmentSettings;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Settings;

[Trait("Category", "Unit")]
public class SaveFulfillmentSettingsValidatorTests
{
    private readonly SaveFulfillmentSettingsValidator _sut = new();

    private static SaveFulfillmentSettingsCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        FbaAutoReplenish: true,
        HepsiAutoReplenish: false);

    [Fact]
    public async Task ValidCommand_ShouldPassValidation()
    {
        var command = CreateValidCommand();

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var command = CreateValidCommand() with { TenantId = Guid.Empty };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task FbaAutoReplenish_True_ShouldPass()
    {
        var command = CreateValidCommand() with { FbaAutoReplenish = true };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task FbaAutoReplenish_False_ShouldPass()
    {
        var command = CreateValidCommand() with { FbaAutoReplenish = false };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task HepsiAutoReplenish_True_ShouldPass()
    {
        var command = CreateValidCommand() with { HepsiAutoReplenish = true };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task HepsiAutoReplenish_False_ShouldPass()
    {
        var command = CreateValidCommand() with { HepsiAutoReplenish = false };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task BothFlags_True_ShouldPass()
    {
        var command = CreateValidCommand() with { FbaAutoReplenish = true, HepsiAutoReplenish = true };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task BothFlags_False_ShouldPass()
    {
        var command = CreateValidCommand() with { FbaAutoReplenish = false, HepsiAutoReplenish = false };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
