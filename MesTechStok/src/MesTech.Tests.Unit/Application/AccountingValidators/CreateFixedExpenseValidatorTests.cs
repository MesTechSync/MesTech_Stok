using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateFixedExpense;

namespace MesTech.Tests.Unit.Application.AccountingValidators;

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class CreateFixedExpenseValidatorTests
{
    private readonly CreateFixedExpenseValidator _validator = new();

    private static CreateFixedExpenseCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Name: "Ofis Kirasi",
        MonthlyAmount: 15000m,
        DayOfMonth: 1,
        StartDate: new DateTime(2026, 1, 1),
        Currency: "TRY",
        EndDate: null,
        SupplierName: null,
        SupplierId: null,
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
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptyName_FailsValidation()
    {
        var cmd = ValidCommand() with { Name = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task NameTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { Name = new string('A', 201) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ZeroMonthlyAmount_FailsValidation()
    {
        var cmd = ValidCommand() with { MonthlyAmount = 0m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task NegativeMonthlyAmount_FailsValidation()
    {
        var cmd = ValidCommand() with { MonthlyAmount = -100m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public async Task InvalidDayOfMonth_FailsValidation(int day)
    {
        var cmd = ValidCommand() with { DayOfMonth = day };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(15)]
    [InlineData(31)]
    public async Task ValidDayOfMonth_PassesValidation(int day)
    {
        var cmd = ValidCommand() with { DayOfMonth = day };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CurrencyTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { Currency = "TRYY" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task EndDateBeforeStartDate_FailsValidation()
    {
        var cmd = ValidCommand() with
        {
            StartDate = new DateTime(2026, 6, 1),
            EndDate = new DateTime(2026, 1, 1)
        };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task EndDateAfterStartDate_PassesValidation()
    {
        var cmd = ValidCommand() with
        {
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31)
        };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task SupplierNameTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { SupplierName = new string('S', 201) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task NotesTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { Notes = new string('N', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }
}
