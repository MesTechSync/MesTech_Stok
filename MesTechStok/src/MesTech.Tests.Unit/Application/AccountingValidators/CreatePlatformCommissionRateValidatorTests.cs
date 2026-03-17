using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreatePlatformCommissionRate;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Application.AccountingValidators;

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class CreatePlatformCommissionRateValidatorTests
{
    private readonly CreatePlatformCommissionRateValidator _validator = new();

    private static CreatePlatformCommissionRateCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Platform: PlatformType.Trendyol,
        Rate: 12.5m,
        Type: CommissionType.Percentage,
        CategoryName: "Elektronik",
        PlatformCategoryId: "CAT-001",
        MinAmount: null,
        MaxAmount: null,
        Currency: "TRY",
        EffectiveFrom: new DateTime(2026, 1, 1),
        EffectiveTo: new DateTime(2026, 12, 31),
        Notes: null
    );

    [Fact]
    public async Task ValidCommand_PassesValidation()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_FailsValidation()
    {
        var cmd = ValidCommand() with { TenantId = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task NegativeRate_FailsValidation()
    {
        var cmd = ValidCommand() with { Rate = -1m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ZeroRate_PassesValidation()
    {
        var cmd = ValidCommand() with { Rate = 0m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CurrencyTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { Currency = "TRYY" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task EmptyCurrency_FailsValidation()
    {
        var cmd = ValidCommand() with { Currency = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task CategoryNameTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { CategoryName = new string('C', 201) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task NotesTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { Notes = new string('N', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task NegativeMinAmount_FailsValidation()
    {
        var cmd = ValidCommand() with { MinAmount = -1m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task NegativeMaxAmount_FailsValidation()
    {
        var cmd = ValidCommand() with { MaxAmount = -1m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task EffectiveToBeforeEffectiveFrom_FailsValidation()
    {
        var cmd = ValidCommand() with
        {
            EffectiveFrom = new DateTime(2026, 12, 31),
            EffectiveTo = new DateTime(2026, 1, 1)
        };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task NullEffectiveDates_PassesValidation()
    {
        var cmd = ValidCommand() with
        {
            EffectiveFrom = null,
            EffectiveTo = null
        };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
