using FluentAssertions;
using MesTech.Application.Commands.RemoveStock;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Stock;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class RemoveStockValidatorTests
{
    private readonly RemoveStockValidator _sut = new();

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
    public async Task Quantity_WhenNegative_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Quantity = -1 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Quantity");
    }

    [Fact]
    public async Task Reason_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Reason = new string('R', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }

    [Fact]
    public async Task DocumentNumber_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { DocumentNumber = new string('D', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DocumentNumber");
    }

    private static RemoveStockCommand CreateValidCommand() => new(
        ProductId: Guid.NewGuid(),
        Quantity: 5,
        Reason: "Damaged goods",
        DocumentNumber: "DOC-002"
    );
}
