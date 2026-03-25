using FluentAssertions;
using MesTech.Application.Commands.AddStock;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class AddStockValidatorTests
{
    private readonly AddStockValidator _validator = new();

    private static AddStockCommand ValidCommand() => new(
        ProductId: Guid.NewGuid(),
        Quantity: 100,
        UnitCost: 50m,
        BatchNumber: "BATCH-001",
        ExpiryDate: DateTime.UtcNow.AddMonths(6),
        DocumentNumber: "DOC-001",
        Reason: "İlk stok girişi");

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
    public void Negative_UnitCost_Fails()
    {
        var cmd = ValidCommand() with { UnitCost = -0.01m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UnitCost");
    }

    [Fact]
    public void Zero_UnitCost_Passes()
    {
        var cmd = ValidCommand() with { UnitCost = 0m };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "UnitCost");
    }

    [Fact]
    public void BatchNumber_Over500_Fails()
    {
        var cmd = ValidCommand() with { BatchNumber = new string('B', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BatchNumber");
    }

    [Fact]
    public void BatchNumber_Null_Passes()
    {
        var cmd = ValidCommand() with { BatchNumber = null };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "BatchNumber");
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
    public void BatchNumber_Exactly500_Passes()
    {
        var cmd = ValidCommand() with { BatchNumber = new string('B', 500) };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "BatchNumber");
    }
}
