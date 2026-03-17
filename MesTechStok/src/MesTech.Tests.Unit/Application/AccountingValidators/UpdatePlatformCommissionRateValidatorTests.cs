using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.UpdatePlatformCommissionRate;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Application.AccountingValidators;

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class UpdatePlatformCommissionRateValidatorTests
{
    private readonly UpdatePlatformCommissionRateValidator _validator = new();

    private static UpdatePlatformCommissionRateCommand ValidCommand() =>
        new(Id: Guid.NewGuid(), Rate: 8.5m, Currency: "TRY");

    [Fact]
    public async Task Valid_Input_Should_Pass()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyId_Should_Fail()
    {
        var cmd = ValidCommand() with { Id = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Fact]
    public async Task NegativeRate_Should_Fail()
    {
        var cmd = ValidCommand() with { Rate = -1m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Rate");
    }

    [Fact]
    public async Task ZeroRate_Should_Pass()
    {
        var cmd = ValidCommand() with { Rate = 0m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CurrencyTooLong_Should_Fail()
    {
        var cmd = ValidCommand() with { Currency = "USDX" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Currency");
    }

    [Fact]
    public async Task NegativeMinAmount_Should_Fail()
    {
        var cmd = ValidCommand() with { MinAmount = -5m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MinAmount");
    }

    [Fact]
    public async Task ValidType_Should_Pass()
    {
        var cmd = ValidCommand() with { Type = CommissionType.Percentage };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
