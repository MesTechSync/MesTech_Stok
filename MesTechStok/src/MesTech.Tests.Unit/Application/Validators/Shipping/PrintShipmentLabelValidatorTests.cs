using FluentAssertions;
using MesTech.Application.Features.Shipping.Commands.PrintShipmentLabel;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Shipping;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class PrintShipmentLabelValidatorTests
{
    private readonly PrintShipmentLabelValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TenantId_Empty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task ShipmentId_Empty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { ShipmentId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ShipmentId");
    }

    [Fact]
    public async Task PrinterName_Null_ShouldPass()
    {
        // PrinterName nullable — null gecerli
        var cmd = CreateValidCommand() with { PrinterName = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task PrinterName_NonNull_Exceeds500_ShouldFail()
    {
        var cmd = CreateValidCommand() with { PrinterName = new string('A', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PrinterName");
    }

    [Fact]
    public async Task PrinterName_Exactly500_ShouldPass()
    {
        var cmd = CreateValidCommand() with { PrinterName = new string('A', 500) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task PrinterName_ShortString_ShouldPass()
    {
        var cmd = CreateValidCommand() with { PrinterName = "HP" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task BothIds_Empty_ShouldFail_WithMultipleErrors()
    {
        var cmd = CreateValidCommand() with { TenantId = Guid.Empty, ShipmentId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(2);
    }

    private static PrintShipmentLabelCommand CreateValidCommand() =>
        new(Guid.NewGuid(), Guid.NewGuid(), "HP LaserJet Pro");
}
