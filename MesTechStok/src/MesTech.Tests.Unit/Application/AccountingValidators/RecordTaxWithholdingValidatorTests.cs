using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.RecordTaxWithholding;

namespace MesTech.Tests.Unit.Application.AccountingValidators;

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class RecordTaxWithholdingValidatorTests
{
    private readonly RecordTaxWithholdingValidator _validator = new();

    private static RecordTaxWithholdingCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        TaxExclusiveAmount: 10_000m,
        Rate: 20m,
        TaxType: "Stopaj",
        InvoiceId: Guid.NewGuid()
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
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task NegativeTaxExclusiveAmount_FailsValidation()
    {
        var cmd = ValidCommand() with { TaxExclusiveAmount = -1m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TaxExclusiveAmount");
    }

    [Fact]
    public async Task ZeroTaxExclusiveAmount_PassesValidation()
    {
        var cmd = ValidCommand() with { TaxExclusiveAmount = 0m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task NegativeRate_FailsValidation()
    {
        var cmd = ValidCommand() with { Rate = -1m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Rate");
    }

    [Fact]
    public async Task ZeroRate_PassesValidation()
    {
        var cmd = ValidCommand() with { Rate = 0m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTaxType_FailsValidation()
    {
        var cmd = ValidCommand() with { TaxType = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TaxType");
    }

    [Fact]
    public async Task TaxTypeTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { TaxType = new string('T', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TaxType");
    }
}
