using FluentAssertions;
using MesTech.Application.Features.System.Commands.TriggerBackup;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.System;

[Trait("Category", "Unit")]
public class TriggerBackupValidatorTests
{
    private readonly TriggerBackupValidator _sut = new();

    private static TriggerBackupCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Description: "Scheduled daily backup");

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
    public async Task NullDescription_ShouldPass()
    {
        var command = CreateValidCommand() with { Description = null };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Description_AtMaxLength_ShouldPass()
    {
        var command = CreateValidCommand() with { Description = new string('d', 500) };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Description_ExceedsMaxLength_ShouldFail()
    {
        var command = CreateValidCommand() with { Description = new string('d', 501) };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public async Task EmptyDescription_ShouldPass()
    {
        var command = CreateValidCommand() with { Description = "" };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task WhitespaceDescription_ShouldPass()
    {
        var command = CreateValidCommand() with { Description = "   " };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidTenantId_WithNullDescription_ShouldPass()
    {
        var command = new TriggerBackupCommand(Guid.NewGuid());

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
