using FluentAssertions;
using MesTech.Application.Features.Finance.Commands.RecordCashTransaction;
using MesTech.Domain.Entities.Finance;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class RecordCashTransactionValidatorTests
{
    private readonly RecordCashTransactionValidator _validator = new();

    private static RecordCashTransactionCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        CashRegisterId: Guid.NewGuid(),
        Type: CashTransactionType.Income,
        Amount: 500m,
        Description: "Nakit satış tahsilatı",
        Category: "Satış");

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
    public void Empty_CashRegisterId_Fails()
    {
        var cmd = ValidCommand() with { CashRegisterId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CashRegisterId");
    }

    [Fact]
    public void Zero_Amount_Fails()
    {
        var cmd = ValidCommand() with { Amount = 0m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public void Negative_Amount_Fails()
    {
        var cmd = ValidCommand() with { Amount = -100m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public void Empty_Description_Fails()
    {
        var cmd = ValidCommand() with { Description = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public void Description_Over500_Fails()
    {
        var cmd = ValidCommand() with { Description = new string('D', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public void Category_Over100_Fails()
    {
        var cmd = ValidCommand() with { Category = new string('C', 101) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Category");
    }

    [Fact]
    public void Category_Null_Passes()
    {
        var cmd = ValidCommand() with { Category = null };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "Category");
    }

    [Fact]
    public void Invalid_Type_Fails()
    {
        var cmd = ValidCommand() with { Type = (CashTransactionType)999 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Type");
    }
}
