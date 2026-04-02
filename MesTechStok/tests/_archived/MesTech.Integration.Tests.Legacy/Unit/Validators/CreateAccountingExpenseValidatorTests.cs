using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateAccountingExpense;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Validators;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateAccountingExpenseValidatorTests
{
    private readonly CreateAccountingExpenseValidator _validator = new();

    private static CreateAccountingExpenseCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Title: "Kargo gideri",
        Amount: 150.50m,
        ExpenseDate: DateTime.UtcNow,
        Source: ExpenseSource.Manual,
        Category: "Lojistik");

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = ValidCommand() with { TenantId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void Empty_Title_Fails()
    {
        var cmd = ValidCommand() with { Title = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public void Title_Over_300_Chars_Fails()
    {
        var cmd = ValidCommand() with { Title = new string('T', 301) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public void Zero_Amount_Fails()
    {
        var cmd = ValidCommand() with { Amount = 0 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public void Negative_Amount_Fails()
    {
        var cmd = ValidCommand() with { Amount = -10m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public void Invalid_Source_Enum_Fails()
    {
        var cmd = ValidCommand() with { Source = (ExpenseSource)999 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Source");
    }

    [Fact]
    public void Category_Over_100_Chars_Fails()
    {
        var cmd = ValidCommand() with { Category = new string('C', 101) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Category");
    }

    [Fact]
    public void Null_Category_Passes()
    {
        var cmd = ValidCommand() with { Category = null };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}
