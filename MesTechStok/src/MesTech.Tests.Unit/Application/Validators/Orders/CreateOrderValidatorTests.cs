using FluentAssertions;
using MesTech.Application.Commands.CreateOrder;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Orders;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CreateOrderValidatorTests
{
    private readonly CreateOrderValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValid();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyCustomerId_ShouldFail()
    {
        var cmd = CreateValid() with { CustomerId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerId");
    }

    [Fact]
    public async Task EmptyCustomerName_ShouldFail()
    {
        var cmd = CreateValid() with { CustomerName = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerName");
    }

    [Fact]
    public async Task CustomerNameExceeds200_ShouldFail()
    {
        var cmd = CreateValid() with { CustomerName = new string('A', 201) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerName");
    }

    [Fact]
    public async Task EmptyOrderType_ShouldFail()
    {
        var cmd = CreateValid() with { OrderType = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderType");
    }

    [Fact]
    public async Task OrderTypeExceeds50_ShouldFail()
    {
        var cmd = CreateValid() with { OrderType = new string('X', 51) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderType");
    }

    [Fact]
    public async Task InvalidEmail_ShouldFail()
    {
        var cmd = CreateValid() with { CustomerEmail = "not-an-email" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerEmail");
    }

    [Fact]
    public async Task NullEmail_ShouldPass()
    {
        var cmd = CreateValid() with { CustomerEmail = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task NotesExceeds2000_ShouldFail()
    {
        var cmd = CreateValid() with { Notes = new string('N', 2001) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Notes");
    }

    [Fact]
    public async Task NullNotes_ShouldPass()
    {
        var cmd = CreateValid() with { Notes = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    private static CreateOrderCommand CreateValid() => new(
        CustomerId: Guid.NewGuid(),
        CustomerName: "Test Müşteri",
        CustomerEmail: "test@example.com",
        OrderType: "MANUAL",
        Notes: "Test sipariş"
    );
}
