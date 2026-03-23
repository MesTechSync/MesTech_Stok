using FluentAssertions;
using MesTech.Application.Commands.TransferStock;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Stock;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class TransferStockValidatorTests
{
    private readonly TransferStockValidator _sut = new();

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
    public async Task SourceWarehouseId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { SourceWarehouseId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SourceWarehouseId");
    }

    [Fact]
    public async Task TargetWarehouseId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TargetWarehouseId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TargetWarehouseId");
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
    public async Task Notes_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Notes = new string('N', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Notes");
    }

    private static TransferStockCommand CreateValidCommand() => new(
        ProductId: Guid.NewGuid(),
        SourceWarehouseId: Guid.NewGuid(),
        TargetWarehouseId: Guid.NewGuid(),
        Quantity: 10,
        Notes: "Transfer between warehouses"
    );
}
