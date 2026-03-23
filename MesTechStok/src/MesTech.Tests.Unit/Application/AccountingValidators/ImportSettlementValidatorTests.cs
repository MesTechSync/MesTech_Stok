using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.ImportSettlement;

namespace MesTech.Tests.Unit.Application.AccountingValidators;

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class ImportSettlementValidatorTests
{
    private readonly ImportSettlementValidator _validator = new();

    private static ImportSettlementCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Platform: "Trendyol",
        PeriodStart: new DateTime(2026, 3, 1),
        PeriodEnd: new DateTime(2026, 3, 15),
        TotalGross: 50_000m,
        TotalCommission: 5_000m,
        TotalNet: 45_000m,
        Lines: new List<SettlementLineInput>
        {
            new(
                OrderId: "ORD-001",
                GrossAmount: 1000m,
                CommissionAmount: 100m,
                ServiceFee: 10m,
                CargoDeduction: 25m,
                RefundDeduction: 0m,
                NetAmount: 865m
            )
        }
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
    public async Task NegativeTotalGross_FailsValidation()
    {
        var cmd = ValidCommand() with { TotalGross = -1m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TotalGross");
    }

    [Fact]
    public async Task ZeroTotalGross_PassesValidation()
    {
        var cmd = ValidCommand() with { TotalGross = 0m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task NegativeTotalCommission_FailsValidation()
    {
        var cmd = ValidCommand() with { TotalCommission = -1m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TotalCommission");
    }

    [Fact]
    public async Task NegativeTotalNet_FailsValidation()
    {
        var cmd = ValidCommand() with { TotalNet = -1m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TotalNet");
    }
}
