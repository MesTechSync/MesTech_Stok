using FluentAssertions;
using MesTech.Application.Commands.CreateIncome;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateIncomeValidatorTests
{
    private readonly CreateIncomeValidator _validator = new();

    private static CreateIncomeCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        StoreId: Guid.NewGuid(),
        Description: "Platform satış geliri",
        Amount: 5000m,
        IncomeType: IncomeType.Satis,
        InvoiceId: Guid.NewGuid(),
        Date: DateTime.UtcNow,
        Note: "Trendyol satışı");

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
    public void Negative_Amount_Fails()
    {
        var cmd = ValidCommand() with { Amount = -1m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public void Zero_Amount_Passes()
    {
        var cmd = ValidCommand() with { Amount = 0m };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public void Invalid_IncomeType_Fails()
    {
        var cmd = ValidCommand() with { IncomeType = (IncomeType)999 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "IncomeType");
    }

    [Fact]
    public void Note_Over500_Fails()
    {
        var cmd = ValidCommand() with { Note = new string('N', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Note");
    }

    [Fact]
    public void Note_Null_Passes()
    {
        var cmd = ValidCommand() with { Note = null };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "Note");
    }
}
