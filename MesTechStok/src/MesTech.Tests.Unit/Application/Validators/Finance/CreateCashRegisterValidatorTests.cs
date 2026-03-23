using FluentAssertions;
using MesTech.Application.Features.Finance.Commands.CreateCashRegister;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Finance;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CreateCashRegisterValidatorTests
{
    private readonly CreateCashRegisterValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptyName_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Name = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task NameExceeds200Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Name = new string('K', 201) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task EmptyCurrencyCode_ShouldFail()
    {
        var cmd = CreateValidCommand() with { CurrencyCode = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CurrencyCode");
    }

    [Fact]
    public async Task CurrencyCodeNot3Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { CurrencyCode = "TR" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CurrencyCode");
    }

    [Fact]
    public async Task CurrencyCode4Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { CurrencyCode = "TRYY" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CurrencyCode");
    }

    [Fact]
    public async Task NegativeOpeningBalance_ShouldFail()
    {
        var cmd = CreateValidCommand() with { OpeningBalance = -1 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OpeningBalance");
    }

    [Fact]
    public async Task ZeroOpeningBalance_ShouldPass()
    {
        var cmd = CreateValidCommand() with { OpeningBalance = 0 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    private static CreateCashRegisterCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Name: "Ana Kasa",
        CurrencyCode: "TRY",
        OpeningBalance: 1000m
    );
}
