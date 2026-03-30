using FluentAssertions;
using MesTech.Application.Features.Shipping.Commands.AutoShipOrder;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Validators;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class AutoShipOrderValidatorTests
{
    private readonly AutoShipOrderValidator _validator = new();

    private static AutoShipOrderCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        OrderId: Guid.NewGuid(),
        AllowManualOverride: false);

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
    public void Empty_OrderId_Fails()
    {
        var cmd = ValidCommand() with { OrderId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderId");
    }

    [Fact]
    public void AllowManualOverride_True_Passes()
    {
        var cmd = ValidCommand() with { AllowManualOverride = true };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}
