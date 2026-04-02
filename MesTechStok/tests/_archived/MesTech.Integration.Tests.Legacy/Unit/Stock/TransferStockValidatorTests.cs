using FluentAssertions;
using MesTech.Application.Commands.TransferStock;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class TransferStockValidatorTests
{
    private readonly TransferStockValidator _validator = new();

    private static TransferStockCommand ValidCommand() => new(
        ProductId: Guid.NewGuid(),
        SourceWarehouseId: Guid.NewGuid(),
        TargetWarehouseId: Guid.NewGuid(),
        Quantity: 25,
        Notes: "Depo arası transfer");

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
    public void Empty_SourceWarehouseId_Fails()
    {
        var cmd = ValidCommand() with { SourceWarehouseId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SourceWarehouseId");
    }

    [Fact]
    public void Empty_TargetWarehouseId_Fails()
    {
        var cmd = ValidCommand() with { TargetWarehouseId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TargetWarehouseId");
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
    public void Notes_Over500_Fails()
    {
        var cmd = ValidCommand() with { Notes = new string('N', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Notes");
    }

    [Fact]
    public void Notes_Null_Passes()
    {
        var cmd = ValidCommand() with { Notes = null };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "Notes");
    }
}
