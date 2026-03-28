using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateSalaryRecord;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateSalaryRecordValidatorTests
{
    private readonly CreateSalaryRecordValidator _validator = new();

    private static CreateSalaryRecordCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        EmployeeName: "Ahmet Yılmaz",
        GrossSalary: 45000m,
        SGKEmployer: 10125m,
        SGKEmployee: 6300m,
        IncomeTax: 7500m,
        StampTax: 342.45m,
        Year: 2026,
        Month: 3);

    [Fact]
    public void Valid_Command_Passes()
    {
        _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = ValidCommand() with { TenantId = Guid.Empty };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Empty_EmployeeName_Fails()
    {
        var cmd = ValidCommand() with { EmployeeName = "" };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void EmployeeName_Over200_Fails()
    {
        var cmd = ValidCommand() with { EmployeeName = new string('A', 201) };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Zero_GrossSalary_Fails()
    {
        var cmd = ValidCommand() with { GrossSalary = 0m };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Negative_SGKEmployer_Fails()
    {
        var cmd = ValidCommand() with { SGKEmployer = -1m };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Negative_SGKEmployee_Fails()
    {
        var cmd = ValidCommand() with { SGKEmployee = -1m };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Negative_IncomeTax_Fails()
    {
        var cmd = ValidCommand() with { IncomeTax = -1m };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Negative_StampTax_Fails()
    {
        var cmd = ValidCommand() with { StampTax = -1m };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Year_1999_Fails()
    {
        var cmd = ValidCommand() with { Year = 1999 };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Year_2101_Fails()
    {
        var cmd = ValidCommand() with { Year = 2101 };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Month_0_Fails()
    {
        var cmd = ValidCommand() with { Month = 0 };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Month_13_Fails()
    {
        var cmd = ValidCommand() with { Month = 13 };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Notes_Over500_Fails()
    {
        var cmd = ValidCommand() with { Notes = new string('N', 501) };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Notes_Null_Passes()
    {
        var cmd = ValidCommand() with { Notes = null };
        _validator.Validate(cmd).Errors.Should().NotContain(e => e.PropertyName == "Notes");
    }
}
