using FluentAssertions;
using MesTech.Application.Features.Stock.Commands.CreateStockLot;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Validators;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateStockLotValidatorTests
{
    private readonly CreateStockLotValidator _validator = new();

    private static CreateStockLotCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        ProductId: Guid.NewGuid(),
        LotNumber: "LOT-2026-001",
        Quantity: 100,
        UnitCost: 25.50m,
        WarehouseId: Guid.NewGuid(),
        WarehouseName: "Ana Depo",
        SupplierId: Guid.NewGuid(),
        SupplierName: "ABC Tedarikçi",
        ExpiryDate: DateTime.UtcNow.AddMonths(12),
        Notes: "İlk parti");

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
    public void Empty_ProductId_Fails()
    {
        var cmd = ValidCommand() with { ProductId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductId");
    }

    [Fact]
    public void Empty_LotNumber_Fails()
    {
        var cmd = ValidCommand() with { LotNumber = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LotNumber");
    }

    [Fact]
    public void LotNumber_Over_50_Chars_Fails()
    {
        var cmd = ValidCommand() with { LotNumber = new string('L', 51) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LotNumber");
    }

    [Fact]
    public void Zero_Quantity_Fails()
    {
        var cmd = ValidCommand() with { Quantity = 0 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Quantity");
    }

    [Fact]
    public void Negative_Quantity_Fails()
    {
        var cmd = ValidCommand() with { Quantity = -5 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Quantity");
    }

    [Fact]
    public void Negative_UnitCost_Fails()
    {
        var cmd = ValidCommand() with { UnitCost = -1m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UnitCost");
    }

    [Fact]
    public void Zero_UnitCost_Passes()
    {
        var cmd = ValidCommand() with { UnitCost = 0m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Null_Optional_Fields_Pass()
    {
        var cmd = ValidCommand() with
        {
            WarehouseId = null,
            WarehouseName = null,
            SupplierId = null,
            SupplierName = null,
            ExpiryDate = null,
            Notes = null
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}
