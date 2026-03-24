using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CloseAccountingPeriod;

namespace MesTech.Tests.Unit.Application.AccountingValidators;

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class CloseAccountingPeriodValidatorTests
{
    private readonly CloseAccountingPeriodValidator _validator = new();

    private static CloseAccountingPeriodCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Year: 2026,
        Month: 3,
        UserId: "admin-user");

    [Fact]
    public async Task ValidCommand_Passes()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_Fails()
    {
        var cmd = ValidCommand() with { TenantId = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task YearTooLow_Fails()
    {
        var cmd = ValidCommand() with { Year = 2019 };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task YearTooHigh_Fails()
    {
        var cmd = ValidCommand() with { Year = 2101 };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task MonthZero_Fails()
    {
        var cmd = ValidCommand() with { Month = 0 };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Month13_Fails()
    {
        var cmd = ValidCommand() with { Month = 13 };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task EmptyUserId_Fails()
    {
        var cmd = ValidCommand() with { UserId = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }
}
