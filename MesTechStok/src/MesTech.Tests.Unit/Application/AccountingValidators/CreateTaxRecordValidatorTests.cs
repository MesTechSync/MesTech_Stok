using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateTaxRecord;

namespace MesTech.Tests.Unit.Application.AccountingValidators;

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
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
        DueDate: new DateTime(2026, 4, 26),
        Year: 2026
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
    public async Task EmptyPeriod_FailsValidation()
    {
        var cmd = ValidCommand() with { Period = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task PeriodTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { Period = new string('P', 21) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task EmptyTaxType_FailsValidation()
    {
        var cmd = ValidCommand() with { TaxType = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task TaxTypeTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { TaxType = new string('T', 51) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task NegativeTaxableAmount_FailsValidation()
    {
        var cmd = ValidCommand() with { TaxableAmount = -1m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ZeroTaxableAmount_PassesValidation()
    {
        var cmd = ValidCommand() with { TaxableAmount = 0m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task InvalidTaxRate_FailsValidation(decimal rate)
    {
        var cmd = ValidCommand() with { TaxRate = rate };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(18)]
    [InlineData(20)]
    [InlineData(100)]
    public async Task ValidTaxRate_PassesValidation(decimal rate)
    {
        var cmd = ValidCommand() with { TaxRate = rate };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task NegativeTaxAmount_FailsValidation()
    {
        var cmd = ValidCommand() with { TaxAmount = -1m };
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

    [Fact]
    public async Task NullYear_PassesValidation()
    {
        var cmd = ValidCommand() with { Year = null };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task AllFieldsValid_WithGelirVergisi_PassesValidation()
    {
        var cmd = ValidCommand() with
        {
            TaxType = "GelirVergisi",
            Period = "2026-Q1",
            TaxRate = 15m,
            TaxableAmount = 50000m,
            TaxAmount = 7500m
        };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
