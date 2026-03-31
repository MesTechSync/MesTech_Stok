using FluentAssertions;
using MesTech.Application.Features.Auth.Commands.VerifyTotp;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Auth;

[Trait("Category", "Unit")]
public class VerifyTotpValidatorTests
{
    private readonly VerifyTotpValidator _sut = new();

    private static VerifyTotpCommand CreateValidCommand() =>
        new(Guid.NewGuid(), "123456");

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
    public async Task Code_WhenEmpty_ShouldFail()
    {
        var command = CreateValidCommand() with { Code = "" };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public async Task Code_WhenNull_ShouldFail()
    {
        var command = CreateValidCommand() with { Code = null! };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public async Task Code_WhenTooShort_ShouldFail()
    {
        var command = CreateValidCommand() with { Code = "12345" };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public async Task Code_WhenTooLong_ShouldFail()
    {
        var command = CreateValidCommand() with { Code = "1234567" };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public async Task Code_WhenNonDigit_ShouldFail()
    {
        var command = CreateValidCommand() with { Code = "abcdef" };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public async Task Code_WhenMixedAlphaNumeric_ShouldFail()
    {
        var command = CreateValidCommand() with { Code = "12ab56" };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public async Task Code_WhenValidSixDigits_ShouldPass()
    {
        var command = CreateValidCommand() with { Code = "999999" };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Code_WhenAllZeros_ShouldPass()
    {
        var command = CreateValidCommand() with { Code = "000000" };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Code_WhenContainsSpaces_ShouldFail()
    {
        var command = CreateValidCommand() with { Code = "123 56" };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task BothFields_WhenInvalid_ShouldFailWithMultipleErrors()
    {
        var command = CreateValidCommand() with { UserId = Guid.Empty, Code = "" };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(2);
    }
}
