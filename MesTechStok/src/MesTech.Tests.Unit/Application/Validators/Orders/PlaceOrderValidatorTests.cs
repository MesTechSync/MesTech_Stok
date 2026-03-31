using FluentAssertions;
using MesTech.Application.Commands.PlaceOrder;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Orders;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class PlaceOrderValidatorTests
{
    private readonly PlaceOrderValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CustomerId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { CustomerId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerId");
    }

    [Fact]
    public async Task CustomerName_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { CustomerName = new string('C', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerName");
    }

    [Fact]
    public async Task CustomerName_WhenNull_ShouldPass()
    {
        var cmd = CreateValidCommand() with { CustomerName = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CustomerEmail_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { CustomerEmail = new string('e', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerEmail");
    }

    [Fact]
    public async Task CustomerEmail_WhenNull_ShouldPass()
    {
        var cmd = CreateValidCommand() with { CustomerEmail = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Notes_WhenExceeds2000Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Notes = new string('N', 2001) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Notes");
    }

    [Fact]
    public async Task Notes_WhenNull_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Notes = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    private static PlaceOrderCommand CreateValidCommand() => new(
        CustomerId: Guid.NewGuid(),
        CustomerName: "Ahmet Yilmaz",
        CustomerEmail: "ahmet@example.com",
        Notes: "Acil siparis",
        Items: new List<PlaceOrderItem>
        {
            new(Guid.NewGuid(), 2, 49.90m)
        }
    );
}
