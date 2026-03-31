using FluentAssertions;
using MesTech.Application.Features.Auth.Commands.DisableMfa;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Auth;

[Trait("Category", "Unit")]
public class DisableMfaValidatorTests
{
    private readonly DisableMfaValidator _sut = new();

    private static DisableMfaCommand CreateValidCommand() =>
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
    public async Task TotpCode_WhenEmpty_ShouldFail()
    {
        var command = CreateValidCommand() with { TotpCode = "" };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TotpCode");
    }

    [Fact]
    public async Task TotpCode_WhenNull_ShouldFail()
    {
        var command = CreateValidCommand() with { TotpCode = null! };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TotpCode");
    }

    [Fact]
    public async Task TotpCode_WhenTooShort_ShouldFail()
    {
        var command = CreateValidCommand() with { TotpCode = "12345" };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TotpCode");
    }

    [Fact]
    public async Task TotpCode_WhenTooLong_ShouldFail()
    {
        var command = CreateValidCommand() with { TotpCode = "123456789" };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TotpCode");
    }

    [Fact]
    public async Task TotpCode_WhenSixChars_ShouldPass()
    {
        var command = CreateValidCommand() with { TotpCode = "654321" };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TotpCode_WhenEightChars_ShouldPass()
    {
        var command = CreateValidCommand() with { TotpCode = "12345678" };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TotpCode_WhenSevenChars_ShouldPass()
    {
        var command = CreateValidCommand() with { TotpCode = "1234567" };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task BothFields_WhenInvalid_ShouldFailWithMultipleErrors()
    {
        var command = CreateValidCommand() with { UserId = Guid.Empty, TotpCode = "" };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(2);
    }
}
