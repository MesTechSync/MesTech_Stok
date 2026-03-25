using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands.PlaceDropshipOrder;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Dropshipping;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class PlaceDropshipOrderValidatorTests
{
    private readonly PlaceDropshipOrderValidator _validator = new();

    private static PlaceDropshipOrderCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        OrderId: Guid.NewGuid(),
        SupplierId: Guid.NewGuid(),
        ProductId: Guid.NewGuid(),
        SupplierOrderRef: "SUP-ORD-001");

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
    public void Empty_SupplierId_Fails()
    {
        var cmd = ValidCommand() with { SupplierId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SupplierId");
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
    public void Empty_SupplierOrderRef_Fails()
    {
        var cmd = ValidCommand() with { SupplierOrderRef = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SupplierOrderRef");
    }

    [Fact]
    public void SupplierOrderRef_Over500_Fails()
    {
        var cmd = ValidCommand() with { SupplierOrderRef = new string('S', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SupplierOrderRef");
    }
}
