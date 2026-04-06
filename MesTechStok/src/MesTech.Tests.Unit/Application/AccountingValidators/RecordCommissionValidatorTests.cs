using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.RecordCommission;

namespace MesTech.Tests.Unit.Application.AccountingValidators;

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class RecordCommissionValidatorTests
{
    private readonly RecordCommissionValidator _validator = new();

    private static RecordCommissionCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Platform: "Trendyol",
        GrossAmount: 1000m,
        CommissionRate: 18.5m,
        CommissionAmount: 185m,
        ServiceFee: 10m,
        OrderId: "ORD-56789",
        Category: "Elektronik"
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
    public async Task EmptyPlatform_FailsValidation()
    {
        var cmd = ValidCommand() with { Platform = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Platform");
    }

    [Fact]
    public async Task PlatformTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { Platform = new string('P', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Platform");
    }

    [Fact]
    public async Task NegativeGrossAmount_FailsValidation()
    {
        var cmd = ValidCommand() with { GrossAmount = -1m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "GrossAmount");
    }

    [Fact]
    public async Task NegativeCommissionRate_FailsValidation()
    {
        var cmd = ValidCommand() with { CommissionRate = -1m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CommissionRate");
    }

    [Fact]
    public async Task NegativeCommissionAmount_FailsValidation()
    {
        var cmd = ValidCommand() with { CommissionAmount = -1m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CommissionAmount");
    }

    [Fact]
    public async Task NegativeServiceFee_FailsValidation()
    {
        var cmd = ValidCommand() with { ServiceFee = -1m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ServiceFee");
    }

    [Fact]
    public async Task ZeroGrossAmount_FailsValidation()
    {
        var cmd = ValidCommand() with { GrossAmount = 0m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "GrossAmount");
    }

    [Fact]
    public async Task OrderIdTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { OrderId = new string('O', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderId");
    }

    [Fact]
    public async Task NullOrderId_PassesValidation()
    {
        var cmd = ValidCommand() with { OrderId = null };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CategoryTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { Category = new string('C', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Category");
    }

    [Fact]
    public async Task NullCategory_PassesValidation()
    {
        var cmd = ValidCommand() with { Category = null };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
