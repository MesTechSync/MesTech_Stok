using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateSalaryRecord;

namespace MesTech.Tests.Unit.Application.AccountingValidators;

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class CreateSalaryRecordValidatorTests
{
    private readonly CreateSalaryRecordValidator _validator = new();

    private static CreateSalaryRecordCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        EmployeeName: "Ahmet Yilmaz",
        GrossSalary: 25000m,
        SGKEmployer: 5575m,
        SGKEmployee: 3500m,
        IncomeTax: 3750m,
        StampTax: 189.75m,
        Year: 2026,
        Month: 3,
        EmployeeId: Guid.NewGuid(),
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
    public async Task EmptyEmployeeName_FailsValidation()
    {
        var cmd = ValidCommand() with { EmployeeName = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task EmployeeNameTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { EmployeeName = new string('A', 201) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ZeroGrossSalary_FailsValidation()
    {
        var cmd = ValidCommand() with { GrossSalary = 0m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task NegativeGrossSalary_FailsValidation()
    {
        var cmd = ValidCommand() with { GrossSalary = -1000m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task NegativeSGKEmployer_FailsValidation()
    {
        var cmd = ValidCommand() with { SGKEmployer = -100m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task NegativeSGKEmployee_FailsValidation()
    {
        var cmd = ValidCommand() with { SGKEmployee = -100m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task NegativeIncomeTax_FailsValidation()
    {
        var cmd = ValidCommand() with { IncomeTax = -100m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task NegativeStampTax_FailsValidation()
    {
        var cmd = ValidCommand() with { StampTax = -1m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    public async Task InvalidYear_FailsValidation(int year)
    {
        var cmd = ValidCommand() with { Year = year };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public async Task InvalidMonth_FailsValidation(int month)
    {
        var cmd = ValidCommand() with { Month = month };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(12)]
    public async Task ValidMonth_PassesValidation(int month)
    {
        var cmd = ValidCommand() with { Month = month };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task NotesTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { Notes = new string('N', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ZeroSGKValues_PassesValidation()
    {
        var cmd = ValidCommand() with
        {
            SGKEmployer = 0m,
            SGKEmployee = 0m,
            IncomeTax = 0m,
            StampTax = 0m
        };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
