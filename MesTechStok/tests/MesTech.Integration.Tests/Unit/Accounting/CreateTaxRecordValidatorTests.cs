using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateTaxRecord;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateTaxRecordValidatorTests
{
    private readonly CreateTaxRecordValidator _validator = new();

    private static CreateTaxRecordCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Period: "2026-03",
        TaxType: "KDV",
        TaxableAmount: 100000m,
        TaxRate: 20m,
        TaxAmount: 20000m,
        DueDate: DateTime.UtcNow.AddDays(30));

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
    public void Empty_Period_Fails()
    {
        var cmd = ValidCommand() with { Period = "" };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Period_Over20_Fails()
    {
        var cmd = ValidCommand() with { Period = new string('P', 21) };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Empty_TaxType_Fails()
    {
        var cmd = ValidCommand() with { TaxType = "" };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void TaxType_Over50_Fails()
    {
        var cmd = ValidCommand() with { TaxType = new string('T', 51) };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Negative_TaxableAmount_Fails()
    {
        var cmd = ValidCommand() with { TaxableAmount = -1m };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void TaxRate_Over100_Fails()
    {
        var cmd = ValidCommand() with { TaxRate = 101m };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void TaxRate_Negative_Fails()
    {
        var cmd = ValidCommand() with { TaxRate = -1m };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Negative_TaxAmount_Fails()
    {
        var cmd = ValidCommand() with { TaxAmount = -1m };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Zero_TaxableAmount_Passes()
    {
        var cmd = ValidCommand() with { TaxableAmount = 0m };
        _validator.Validate(cmd).Errors.Should().NotContain(e => e.PropertyName == "TaxableAmount");
    }
}
