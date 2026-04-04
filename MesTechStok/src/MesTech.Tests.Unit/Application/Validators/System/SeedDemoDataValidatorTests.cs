using FluentAssertions;
using MesTech.Application.Commands.SeedDemoData;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.System;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class SeedDemoDataValidatorTests
{
    private readonly SeedDemoDataValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new SeedDemoDataCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task DefaultCommand_ShouldProduceNoErrors()
    {
        var cmd = new SeedDemoDataCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.Errors.Should().BeEmpty();
    }
}
