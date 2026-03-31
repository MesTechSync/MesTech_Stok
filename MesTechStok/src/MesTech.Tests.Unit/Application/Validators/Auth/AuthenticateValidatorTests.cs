using FluentAssertions;
using MesTech.Application.Features.Auth.Commands.Authenticate;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Auth;

[Trait("Category", "Unit")]
public class AuthenticateValidatorTests
{
    private readonly AuthenticateValidator _sut = new();

    private static AuthenticateCommand CreateValidCommand() =>
        new("admin", "P@ssw0rd123");

    [Fact]
    public async Task ValidCommand_ShouldPassValidation()
    {
        var command = CreateValidCommand();
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Username_WhenEmpty_ShouldFail()
    {
        var command = CreateValidCommand() with { Username = "" };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Username");
    }

    [Fact]
    public async Task Username_WhenNull_ShouldFail()
    {
        var command = CreateValidCommand() with { Username = null! };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Username");
    }

    [Fact]
    public async Task Username_WhenExceedsMaxLength_ShouldFail()
    {
        var command = CreateValidCommand() with { Username = new string('a', 101) };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Username");
    }

    [Fact]
    public async Task Username_WhenAtMaxLength_ShouldPass()
    {
        var command = CreateValidCommand() with { Username = new string('a', 100) };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Password_WhenEmpty_ShouldFail()
    {
        var command = CreateValidCommand() with { Password = "" };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public async Task Password_WhenNull_ShouldFail()
    {
        var command = CreateValidCommand() with { Password = null! };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public async Task Password_WhenExceedsMaxLength_ShouldFail()
    {
        var command = CreateValidCommand() with { Password = new string('x', 257) };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public async Task Password_WhenAtMaxLength_ShouldPass()
    {
        var command = CreateValidCommand() with { Password = new string('x', 256) };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task BothFields_WhenEmpty_ShouldFailWithMultipleErrors()
    {
        var command = CreateValidCommand() with { Username = "", Password = "" };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(2);
    }
}
