using FluentAssertions;
using MesTech.Application.Commands.AdjustStock;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class AdjustStockValidatorTests
{
    private readonly AdjustStockValidator _validator = new();

    private static AdjustStockCommand ValidCommand() => new(
        ProductId: Guid.NewGuid(),
        Quantity: 50,
        Reason: "Sayım farkı düzeltme",
        PerformedBy: "admin@mestech.com");

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_ProductId_Fails()
    {
        var cmd = ValidCommand() with { ProductId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductId");
    }

    [Fact]
    public void Negative_Quantity_Passes_ForCorrection()
    {
        // Stok düzeltme: negatif miktar kabul edilir (stok azaltma)
        var cmd = ValidCommand() with { Quantity = -1 };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "Quantity");
    }

    [Fact]
    public void Zero_Quantity_Fails_CannotAdjustByZero()
    {
        // Validator değişti: adjustment quantity 0 olamaz (anlamlı değişiklik gerekli)
        var cmd = ValidCommand() with { Quantity = 0 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Quantity");
    }

    [Fact]
    public void Reason_Over500_Fails()
    {
        var cmd = ValidCommand() with { Reason = new string('R', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }

    [Fact]
    public void Reason_Null_Passes()
    {
        var cmd = ValidCommand() with { Reason = null };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "Reason");
    }

    [Fact]
    public void PerformedBy_Over500_Fails()
    {
        var cmd = ValidCommand() with { PerformedBy = new string('P', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PerformedBy");
    }

    [Fact]
    public void PerformedBy_Null_Passes()
    {
        var cmd = ValidCommand() with { PerformedBy = null };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "PerformedBy");
    }
}
