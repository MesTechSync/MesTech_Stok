using FluentAssertions;
using MesTech.Application.DTOs.Cargo;
using MesTech.Domain.Enums;
using MesTech.Domain.ValueObjects;

namespace MesTech.Tests.Unit.Domain;

[Trait("Category", "Unit")]
[Trait("Phase", "Dalga3")]
public class ShipmentDtoTests
{
    [Fact]
    public void ShipmentRequest_DefaultParcelCount_ShouldBe1()
    {
        var request = new ShipmentRequest();
        request.ParcelCount.Should().Be(1);
    }

    [Fact]
    public void ShipmentRequest_CodAmount_ShouldBeNullable()
    {
        var request = new ShipmentRequest { CodAmount = null };
        request.CodAmount.Should().BeNull();

        request.CodAmount = 150.50m;
        request.CodAmount.Should().Be(150.50m);
    }

    [Fact]
    public void ShipmentResult_SuccessCase_ShouldHaveTrackingNumber()
    {
        var result = new ShipmentResult
        {
            Success = true,
            TrackingNumber = "YK-12345678",
            ShipmentId = "SHP-001"
        };

        result.Success.Should().BeTrue();
        result.TrackingNumber.Should().NotBeNullOrEmpty();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ShipmentResult_FailureCase_ShouldHaveErrorMessage()
    {
        var result = new ShipmentResult
        {
            Success = false,
            ErrorMessage = "Adres bulunamadi"
        };

        result.Success.Should().BeFalse();
        result.TrackingNumber.Should().BeNull();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void TrackingResult_Events_ShouldBeChronological()
    {
        var result = new TrackingResult
        {
            TrackingNumber = "YK-12345678",
            Status = CargoStatus.InTransit,
            Events = new List<TrackingEvent>
            {
                new() { Timestamp = new DateTime(2026, 3, 8, 10, 0, 0, DateTimeKind.Utc), Status = CargoStatus.Created, Location = "Istanbul", Description = "Gonderi olusturuldu" },
                new() { Timestamp = new DateTime(2026, 3, 8, 14, 0, 0, DateTimeKind.Utc), Status = CargoStatus.PickedUp, Location = "Istanbul Sube", Description = "Kurye teslim aldi" },
                new() { Timestamp = new DateTime(2026, 3, 9, 8, 0, 0, DateTimeKind.Utc), Status = CargoStatus.InTransit, Location = "Ankara Transfer", Description = "Transfer merkezinde" },
            }
        };

        result.Events.Should().HaveCount(3);
        result.Events.Should().BeInAscendingOrder(e => e.Timestamp);
    }

    [Fact]
    public void Address_DefaultCountry_ShouldBeTR()
    {
        var address = new Address();
        address.Country.Should().Be("TR");
    }
}
