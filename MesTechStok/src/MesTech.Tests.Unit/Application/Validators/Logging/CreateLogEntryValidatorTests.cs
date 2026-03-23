using FluentAssertions;
using MesTech.Application.Features.Logging.Commands.CreateLogEntry;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Logging;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CreateLogEntryValidatorTests
{
    private readonly CreateLogEntryValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TenantId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task Level_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Level = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Level");
    }

    [Fact]
    public async Task Level_WhenInvalid_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Level = "Trace" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Level");
    }

    [Theory]
    [InlineData("Info")]
    [InlineData("Warning")]
    [InlineData("Error")]
    [InlineData("Debug")]
    public async Task Level_WhenValid_ShouldPass(string level)
    {
        var cmd = CreateValidCommand() with { Level = level };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Level_WhenExceeds20Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Level = new string('X', 21) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Level");
    }

    [Fact]
    public async Task Category_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Category = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Category");
    }

    [Fact]
    public async Task Category_WhenExceeds100Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Category = new string('C', 101) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Category");
    }

    [Fact]
    public async Task Message_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Message = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Message");
    }

    [Fact]
    public async Task Message_WhenExceeds4000Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Message = new string('M', 4001) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Message");
    }

    [Fact]
    public async Task Message_WhenExactly4000Chars_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Message = new string('M', 4000) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    private static CreateLogEntryCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Level: "Info",
        Category: "StockSync",
        Message: "Stock synchronized successfully"
    );
}
