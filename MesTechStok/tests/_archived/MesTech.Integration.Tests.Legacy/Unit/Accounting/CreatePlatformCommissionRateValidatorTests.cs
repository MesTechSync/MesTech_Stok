using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreatePlatformCommissionRate;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreatePlatformCommissionRateValidatorTests
{
    private readonly CreatePlatformCommissionRateValidator _validator = new();

    private static CreatePlatformCommissionRateCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Platform: PlatformType.Trendyol,
        Rate: 15.5m,
        Type: CommissionType.Percentage,
        CategoryName: "Elektronik",
        Currency: "TRY",
        EffectiveFrom: new DateTime(2026, 1, 1),
        EffectiveTo: new DateTime(2026, 12, 31),
        Notes: "2026 yılı komisyon oranı");

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = ValidCommand() with { TenantId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void Invalid_Platform_Fails()
    {
        var cmd = ValidCommand() with { Platform = (PlatformType)999 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Platform");
    }

    [Fact]
    public void Negative_Rate_Fails()
    {
        var cmd = ValidCommand() with { Rate = -1m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Zero_Rate_Passes()
    {
        var cmd = ValidCommand() with { Rate = 0m };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "Rate");
    }

    [Fact]
    public void Invalid_CommissionType_Fails()
    {
        var cmd = ValidCommand() with { Type = (CommissionType)999 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Type");
    }

    [Fact]
    public void Empty_Currency_Fails()
    {
        var cmd = ValidCommand() with { Currency = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Currency");
    }

    [Fact]
    public void Currency_Over3_Fails()
    {
        var cmd = ValidCommand() with { Currency = "ABCD" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Currency");
    }

    [Fact]
    public void CategoryName_Over200_Fails()
    {
        var cmd = ValidCommand() with { CategoryName = new string('C', 201) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CategoryName");
    }

    [Fact]
    public void Notes_Over500_Fails()
    {
        var cmd = ValidCommand() with { Notes = new string('N', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Notes");
    }

    [Fact]
    public void Negative_MinAmount_Fails()
    {
        var cmd = ValidCommand() with { MinAmount = -1m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Negative_MaxAmount_Fails()
    {
        var cmd = ValidCommand() with { MaxAmount = -1m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void EffectiveTo_Before_EffectiveFrom_Fails()
    {
        var cmd = ValidCommand() with
        {
            EffectiveFrom = new DateTime(2026, 6, 1),
            EffectiveTo = new DateTime(2026, 1, 1)
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void EffectiveTo_Same_As_EffectiveFrom_Passes()
    {
        var date = new DateTime(2026, 6, 1);
        var cmd = ValidCommand() with { EffectiveFrom = date, EffectiveTo = date };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.ErrorMessage.Contains("EffectiveTo"));
    }

    [Fact]
    public void Null_EffectiveDates_Passes()
    {
        var cmd = ValidCommand() with { EffectiveFrom = null, EffectiveTo = null };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.ErrorMessage.Contains("EffectiveTo"));
    }
}
