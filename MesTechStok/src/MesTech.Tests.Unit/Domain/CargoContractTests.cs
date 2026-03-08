using FluentAssertions;
using MesTech.Application.DTOs.Cargo;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain;

[Trait("Category", "Unit")]
public class CargoContractTests
{
    [Fact]
    public void ShipmentResult_Failed_ShouldSetErrorMessage()
    {
        var result = ShipmentResult.Failed("Connection timeout");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Connection timeout");
        result.TrackingNumber.Should().BeNull();
    }

    [Fact]
    public void ShipmentResult_Succeeded_ShouldSetTrackingNumber()
    {
        var result = ShipmentResult.Succeeded("YK123456789", "SHP-001", "https://label.url/pdf");

        result.Success.Should().BeTrue();
        result.TrackingNumber.Should().Be("YK123456789");
        result.ShipmentId.Should().Be("SHP-001");
        result.LabelUrl.Should().Be("https://label.url/pdf");
    }

    [Fact]
    public void ShipmentRequest_DefaultParcelCount_ShouldBe1()
    {
        var request = new ShipmentRequest();
        request.ParcelCount.Should().Be(1);
    }

    [Fact]
    public void ShipmentRequest_OrderId_ShouldBeGuid()
    {
        var id = Guid.NewGuid();
        var request = new ShipmentRequest { OrderId = id };
        request.OrderId.Should().Be(id);
    }

    [Fact]
    public void TrackingResult_Events_ShouldDefaultToEmptyList()
    {
        var result = new TrackingResult();
        result.Events.Should().BeEmpty();
    }

    [Fact]
    public void ICargoAdapter_ShouldDefine5Methods()
    {
        var methods = typeof(ICargoAdapter).GetMethods()
            .Where(m => !m.IsSpecialName)
            .Select(m => m.Name)
            .ToList();

        methods.Should().Contain("CreateShipmentAsync");
        methods.Should().Contain("TrackShipmentAsync");
        methods.Should().Contain("CancelShipmentAsync");
        methods.Should().Contain("GetShipmentLabelAsync");
        methods.Should().Contain("IsAvailableAsync");
    }

    [Fact]
    public void ICargoAdapter_ShouldDefine5Properties()
    {
        var props = typeof(ICargoAdapter).GetProperties().Select(p => p.Name).ToList();

        props.Should().Contain("Provider");
        props.Should().Contain("SupportsCancellation");
        props.Should().Contain("SupportsLabelGeneration");
        props.Should().Contain("SupportsCashOnDelivery");
        props.Should().Contain("SupportsMultiParcel");
    }
}
