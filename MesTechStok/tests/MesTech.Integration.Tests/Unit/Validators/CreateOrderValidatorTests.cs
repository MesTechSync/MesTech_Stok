using FluentAssertions;
using MesTech.Application.Commands.CreateOrder;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Validators;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateOrderValidatorTests
{
    private readonly CreateOrderValidator _validator = new();

    private static CreateOrderCommand ValidCommand() => new(
        CustomerId: Guid.NewGuid(),
        CustomerName: "Test Müşteri",
        CustomerEmail: "test@example.com",
        OrderType: "STANDARD",
        Notes: "Test sipariş notu");

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_CustomerId_Fails()
    {
        var cmd = ValidCommand() with { CustomerId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerId");
    }

    [Fact]
    public void Empty_CustomerName_Fails()
    {
        var cmd = ValidCommand() with { CustomerName = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerName");
    }

    [Fact]
    public void CustomerName_Over_200_Chars_Fails()
    {
        var cmd = ValidCommand() with { CustomerName = new string('A', 201) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerName");
    }

    [Fact]
    public void Invalid_Email_Fails()
    {
        var cmd = ValidCommand() with { CustomerEmail = "not-an-email" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerEmail");
    }

    [Fact]
    public void Null_Email_Passes()
    {
        var cmd = ValidCommand() with { CustomerEmail = null };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_OrderType_Fails()
    {
        var cmd = ValidCommand() with { OrderType = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderType");
    }

    [Fact]
    public void OrderType_Over_50_Chars_Fails()
    {
        var cmd = ValidCommand() with { OrderType = new string('X', 51) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderType");
    }

    [Fact]
    public void Notes_Over_2000_Chars_Fails()
    {
        var cmd = ValidCommand() with { Notes = new string('N', 2001) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Notes");
    }

    [Fact]
    public void Null_Notes_Passes()
    {
        var cmd = ValidCommand() with { Notes = null };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}
