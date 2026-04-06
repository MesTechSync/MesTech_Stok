using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.RecordCargoExpense;

namespace MesTech.Tests.Unit.Application.AccountingValidators;

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class RecordCargoExpenseValidatorTests
{
    private readonly RecordCargoExpenseValidator _validator = new();

    private static RecordCargoExpenseCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        CarrierName: "Yurtici Kargo",
        Cost: 35.50m,
        OrderId: "ORD-12345",
        TrackingNumber: "YK-987654321"
    );

    [Fact]
    public async Task ValidCommand_PassesValidation()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_FailsValidation()
    {
        var cmd = ValidCommand() with { TenantId = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptyCarrierName_FailsValidation()
    {
        var cmd = ValidCommand() with { CarrierName = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CarrierName");
    }

    [Fact]
    public async Task CarrierNameTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { CarrierName = new string('C', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CarrierName");
    }

    [Fact]
    public async Task NegativeCost_FailsValidation()
    {
        var cmd = ValidCommand() with { Cost = -1m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Cost");
    }

    [Fact]
    public async Task ZeroCost_FailsValidation()
    {
        var cmd = ValidCommand() with { Cost = 0m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Cost");
    }

    [Fact]
    public async Task OrderIdTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { OrderId = new string('O', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderId");
    }

    [Fact]
    public async Task NullOrderId_PassesValidation()
    {
        var cmd = ValidCommand() with { OrderId = null };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TrackingNumberTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { TrackingNumber = new string('T', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TrackingNumber");
    }

    [Fact]
    public async Task NullTrackingNumber_PassesValidation()
    {
        var cmd = ValidCommand() with { TrackingNumber = null };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
