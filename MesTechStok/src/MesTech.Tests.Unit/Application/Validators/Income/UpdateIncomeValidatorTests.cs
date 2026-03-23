using FluentAssertions;
using MesTech.Application.Commands.UpdateIncome;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Income;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class UpdateIncomeValidatorTests
{
    private readonly UpdateIncomeValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Id_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Id = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Fact]
    public async Task Description_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Description = new string('D', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public async Task Description_WhenNull_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Description = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Description_WhenExactly500Chars_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Description = new string('D', 500) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Note_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Note = new string('N', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Note");
    }

    [Fact]
    public async Task Note_WhenNull_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Note = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Note_WhenExactly500Chars_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Note = new string('N', 500) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    private static UpdateIncomeCommand CreateValidCommand() => new(
        Id: Guid.NewGuid(),
        Description: "Updated income",
        Note: "Updated note"
    );
}
