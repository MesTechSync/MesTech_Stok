using FluentAssertions;
using MesTech.Application.Commands.AddStockLot;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Stock;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class AddStockLotValidatorTests
{
    private readonly AddStockLotValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ProductId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { ProductId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductId");
    }

    [Fact]
    public async Task LotNumber_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { LotNumber = string.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LotNumber");
    }

    [Fact]
    public async Task LotNumber_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { LotNumber = new string('L', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LotNumber");
    }

    [Fact]
    public async Task Quantity_WhenNegative_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Quantity = -1 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Quantity");
    }

    [Fact]
    public async Task Quantity_WhenZero_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Quantity = 0 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task UnitCost_WhenNegative_ShouldFail()
    {
        var cmd = CreateValidCommand() with { UnitCost = -1m };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UnitCost");
    }

    [Fact]
    public async Task UnitCost_WhenZero_ShouldPass()
    {
        var cmd = CreateValidCommand() with { UnitCost = 0m };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    private static AddStockLotCommand CreateValidCommand() => new(
        ProductId: Guid.NewGuid(),
        LotNumber: "LOT-2026-001",
        Quantity: 100,
        UnitCost: 15.50m
    );
}
