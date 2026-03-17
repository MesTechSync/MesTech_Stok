using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateAccountingExpense;
using MesTech.Domain.Accounting.Enums;

namespace MesTech.Tests.Unit.Application.AccountingValidators;

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class CreateAccountingExpenseValidatorTests
{
    private readonly CreateAccountingExpenseValidator _validator = new();

    private static CreateAccountingExpenseCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Title: "Kargo masrafi",
        Amount: 1250m,
        ExpenseDate: new DateTime(2026, 3, 15),
        Source: ExpenseSource.Manual,
        Category: "Lojistik"
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
    public async Task EmptyTitle_FailsValidation()
    {
        var cmd = ValidCommand() with { Title = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task TitleTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { Title = new string('T', 301) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ZeroAmount_FailsValidation()
    {
        var cmd = ValidCommand() with { Amount = 0m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task NegativeAmount_FailsValidation()
    {
        var cmd = ValidCommand() with { Amount = -500m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task CategoryTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { Category = new string('C', 101) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task NullCategory_PassesValidation()
    {
        var cmd = ValidCommand() with { Category = null };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(ExpenseSource.Manual)]
    [InlineData(ExpenseSource.WhatsApp)]
    [InlineData(ExpenseSource.AI)]
    public async Task ValidSource_PassesValidation(ExpenseSource source)
    {
        var cmd = ValidCommand() with { Source = source };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
