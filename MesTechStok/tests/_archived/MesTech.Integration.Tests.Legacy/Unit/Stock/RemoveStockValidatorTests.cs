using FluentAssertions;
using MesTech.Application.Commands.RemoveStock;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class RemoveStockValidatorTests
{
    private readonly RemoveStockValidator _validator = new();

    private static RemoveStockCommand ValidCommand() => new(
        ProductId: Guid.NewGuid(),
        Quantity: 10,
        Reason: "Sipariş karşılama",
        DocumentNumber: "SIP-001");

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
    public void Negative_Quantity_Fails()
    {
        var cmd = ValidCommand() with { Quantity = -1 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Quantity");
    }

    [Fact]
    public void Zero_Quantity_Passes()
    {
        var cmd = ValidCommand() with { Quantity = 0 };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "Quantity");
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
    public void DocumentNumber_Over500_Fails()
    {
        var cmd = ValidCommand() with { DocumentNumber = new string('D', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DocumentNumber");
    }

    [Fact]
    public void DocumentNumber_Null_Passes()
    {
        var cmd = ValidCommand() with { DocumentNumber = null };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "DocumentNumber");
    }
}
