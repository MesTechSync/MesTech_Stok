using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.RecordCommission;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class RecordCommissionValidatorTests
{
    private readonly RecordCommissionValidator _validator = new();

    private static RecordCommissionCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Platform: "Trendyol",
        GrossAmount: 1000m,
        CommissionRate: 0.15m,
        CommissionAmount: 150m,
        ServiceFee: 5m,
        OrderId: "ORD-12345",
        Category: "Elektronik");

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
    public void Empty_Platform_Fails()
    {
        var cmd = ValidCommand() with { Platform = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Platform");
    }

    [Fact]
    public void Platform_Over500_Fails()
    {
        var cmd = ValidCommand() with { Platform = new string('A', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Platform");
    }

    [Fact]
    public void Negative_GrossAmount_Fails()
    {
        var cmd = ValidCommand() with { GrossAmount = -1m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "GrossAmount");
    }

    [Fact]
    public void Zero_GrossAmount_Passes()
    {
        var cmd = ValidCommand() with { GrossAmount = 0m };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "GrossAmount");
    }

    [Fact]
    public void Negative_CommissionRate_Fails()
    {
        var cmd = ValidCommand() with { CommissionRate = -0.01m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CommissionRate");
    }

    [Fact]
    public void Negative_CommissionAmount_Fails()
    {
        var cmd = ValidCommand() with { CommissionAmount = -10m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CommissionAmount");
    }

    [Fact]
    public void Negative_ServiceFee_Fails()
    {
        var cmd = ValidCommand() with { ServiceFee = -1m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ServiceFee");
    }

    [Fact]
    public void OrderId_Over500_Fails()
    {
        var cmd = ValidCommand() with { OrderId = new string('X', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderId");
    }

    [Fact]
    public void Category_Over500_Fails()
    {
        var cmd = ValidCommand() with { Category = new string('X', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Category");
    }

    [Fact]
    public void Null_OrderId_Passes()
    {
        var cmd = ValidCommand() with { OrderId = null };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "OrderId");
    }

    [Fact]
    public void Null_Category_Passes()
    {
        var cmd = ValidCommand() with { Category = null };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "Category");
    }
}
