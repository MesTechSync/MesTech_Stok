using FluentAssertions;
using MesTech.Application.Features.Stock.Commands.CreateStockLot;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Stock;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CreateStockLotValidatorTests
{
    private readonly CreateStockLotValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptyProductId_ShouldFail()
    {
        var cmd = CreateValidCommand() with { ProductId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductId");
    }

    [Fact]
    public async Task EmptyLotNumber_ShouldFail()
    {
        var cmd = CreateValidCommand() with { LotNumber = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LotNumber");
    }

    [Fact]
    public async Task LotNumberExceeds50_ShouldFail()
    {
        var cmd = CreateValidCommand() with { LotNumber = new string('L', 51) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LotNumber");
    }

    [Fact]
    public async Task LotNumberExactly50_ShouldPass()
    {
        var cmd = CreateValidCommand() with { LotNumber = new string('L', 50) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task QuantityZero_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Quantity = 0 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Quantity");
    }

    [Fact]
    public async Task QuantityNegative_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Quantity = -5 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Quantity");
    }

    [Fact]
    public async Task QuantityOne_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Quantity = 1 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task UnitCostNegative_ShouldFail()
    {
        var cmd = CreateValidCommand() with { UnitCost = -0.01m };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UnitCost");
    }

    [Fact]
    public async Task UnitCostZero_ShouldPass()
    {
        var cmd = CreateValidCommand() with { UnitCost = 0m };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task OptionalFieldsNull_ShouldPass()
    {
        var cmd = CreateValidCommand() with
        {
            WarehouseId = null,
            WarehouseName = null,
            SupplierId = null,
            SupplierName = null,
            ExpiryDate = null,
            Notes = null
        };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task MultipleInvalidFields_ShouldReportAll()
    {
        var cmd = CreateValidCommand() with
        {
            TenantId = Guid.Empty,
            ProductId = Guid.Empty,
            LotNumber = "",
            Quantity = -1,
            UnitCost = -10m
        };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(5);
    }

    private static CreateStockLotCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        ProductId: Guid.NewGuid(),
        LotNumber: "LOT-2026-001",
        Quantity: 100,
        UnitCost: 45.50m,
        WarehouseId: Guid.NewGuid(),
        WarehouseName: "Ana Depo",
        SupplierId: Guid.NewGuid(),
        SupplierName: "Tedarikci A",
        ExpiryDate: DateTime.UtcNow.AddMonths(6),
        Notes: "Ilk parti");
}
