using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateFixedExpense;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateFixedExpenseValidatorTests
{
    private readonly CreateFixedExpenseValidator _validator = new();

    private static CreateFixedExpenseCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Name: "Kira",
        MonthlyAmount: 25000m,
        DayOfMonth: 1,
        StartDate: DateTime.UtcNow);

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
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Empty_Name_Fails()
    {
        var cmd = ValidCommand() with { Name = "" };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Name_Over200_Fails()
    {
        var cmd = ValidCommand() with { Name = new string('K', 201) };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Zero_MonthlyAmount_Fails()
    {
        var cmd = ValidCommand() with { MonthlyAmount = 0m };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Negative_MonthlyAmount_Fails()
    {
        var cmd = ValidCommand() with { MonthlyAmount = -100m };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void DayOfMonth_Zero_Fails()
    {
        var cmd = ValidCommand() with { DayOfMonth = 0 };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void DayOfMonth_32_Fails()
    {
        var cmd = ValidCommand() with { DayOfMonth = 32 };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void DayOfMonth_31_Passes()
    {
        var cmd = ValidCommand() with { DayOfMonth = 31 };
        _validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Currency_Over3_Fails()
    {
        var cmd = ValidCommand() with { Currency = "ABCD" };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void SupplierName_Over200_Fails()
    {
        var cmd = ValidCommand() with { SupplierName = new string('S', 201) };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Notes_Over500_Fails()
    {
        var cmd = ValidCommand() with { Notes = new string('N', 501) };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void EndDate_Before_StartDate_Fails()
    {
        var cmd = ValidCommand() with
        {
            StartDate = new DateTime(2026, 6, 1),
            EndDate = new DateTime(2026, 1, 1)
        };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void EndDate_After_StartDate_Passes()
    {
        var cmd = ValidCommand() with
        {
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31)
        };
        _validator.Validate(cmd).IsValid.Should().BeTrue();
    }
}
