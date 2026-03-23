using FluentAssertions;
using MesTech.Application.DTOs.Fulfillment;
using MesTech.Application.Features.Fulfillment.Commands.CreateInboundShipment;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Fulfillment;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CreateInboundShipmentValidatorTests
{
    private readonly CreateInboundShipmentValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyShipmentName_ShouldFail()
    {
        var cmd = CreateValidCommand() with { ShipmentName = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ShipmentName");
    }

    [Fact]
    public async Task ShipmentNameExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { ShipmentName = new string('S', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ShipmentName");
    }

    [Fact]
    public async Task NotesExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Notes = new string('N', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Notes");
    }

    [Fact]
    public async Task NotesNull_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Notes = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    private static CreateInboundShipmentCommand CreateValidCommand() => new(
        Center: FulfillmentCenter.AmazonFBA,
        ShipmentName: "FBA-2026-001",
        Items: new[] { new InboundItem("SKU-001", 100) },
        Notes: "Ilk sevkiyat"
    );
}
