using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CloseAccountingPeriod;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CloseAccountingPeriodValidatorTests
{
    private readonly CloseAccountingPeriodValidator _validator = new();

    private static CloseAccountingPeriodCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Year: 2026,
        Month: 3,
        UserId: "admin@mestech.com");

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
    public void Year_Below2020_Fails()
    {
        var cmd = ValidCommand() with { Year = 2019 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Year");
    }

    [Fact]
    public void Year_Above2100_Fails()
    {
        var cmd = ValidCommand() with { Year = 2101 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Year");
    }

    [Fact]
    public void Month_Zero_Fails()
    {
        var cmd = ValidCommand() with { Month = 0 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Month");
    }

    [Fact]
    public void Month_13_Fails()
    {
        var cmd = ValidCommand() with { Month = 13 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Month");
    }

    [Fact]
    public void Month_1_Passes()
    {
        var cmd = ValidCommand() with { Month = 1 };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "Month");
    }

    [Fact]
    public void Month_12_Passes()
    {
        var cmd = ValidCommand() with { Month = 12 };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "Month");
    }

    [Fact]
    public void Empty_UserId_Fails()
    {
        var cmd = ValidCommand() with { UserId = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }
}
