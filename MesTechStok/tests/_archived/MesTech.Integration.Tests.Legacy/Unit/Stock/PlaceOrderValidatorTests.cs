using FluentAssertions;
using MesTech.Application.Commands.PlaceOrder;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class PlaceOrderValidatorTests
{
    private readonly PlaceOrderValidator _validator = new();

    private static PlaceOrderCommand ValidCommand() => new(
        CustomerId: Guid.NewGuid(),
        CustomerName: "Test Müşteri",
        CustomerEmail: "test@example.com",
        Notes: "Test sipariş notu",
        Items: new List<PlaceOrderItem>
        {
            new(Guid.NewGuid(), Quantity: 2, UnitPrice: 100m, TaxRate: 0.18m)
        });

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
    public void CustomerName_Over500_Fails()
    {
        var cmd = ValidCommand() with { CustomerName = new string('A', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerName");
    }

    [Fact]
    public void CustomerName_Null_Passes()
    {
        var cmd = ValidCommand() with { CustomerName = null };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "CustomerName");
    }

    [Fact]
    public void CustomerEmail_Over500_Fails()
    {
        var cmd = ValidCommand() with { CustomerEmail = new string('a', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerEmail");
    }

    [Fact]
    public void CustomerEmail_Null_Passes()
    {
        var cmd = ValidCommand() with { CustomerEmail = null };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "CustomerEmail");
    }

    [Fact]
    public void Notes_Over500_PassesNow()
    {
        // Validator güncellendi — Notes MaxLength kaldırıldı
        var cmd = ValidCommand() with { Notes = new string('N', 501) };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "Notes");
    }

    [Fact]
    public void Notes_Null_Passes()
    {
        var cmd = ValidCommand() with { Notes = null };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "Notes");
    }

    [Fact]
    public void CustomerName_Exactly500_Passes()
    {
        var cmd = ValidCommand() with { CustomerName = new string('A', 500) };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "CustomerName");
    }
}
