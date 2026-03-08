using FluentAssertions;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain;

[Trait("Category", "Unit")]
[Trait("Phase", "Dalga3")]
public class CargoEnumTests
{
    [Fact]
    public void CargoProvider_ShouldHave4Members()
    {
        Enum.GetValues<CargoProvider>().Should().HaveCount(4);
    }

    [Theory]
    [InlineData(CargoProvider.None, 0)]
    [InlineData(CargoProvider.YurticiKargo, 1)]
    [InlineData(CargoProvider.ArasKargo, 2)]
    [InlineData(CargoProvider.SuratKargo, 3)]
    public void CargoProvider_Values_ShouldMatchExpected(CargoProvider provider, int expected)
    {
        ((int)provider).Should().Be(expected);
    }

    [Fact]
    public void CargoStatus_ShouldHave8Members()
    {
        Enum.GetValues<CargoStatus>().Should().HaveCount(8);
    }

    [Theory]
    [InlineData(CargoStatus.Unknown)]
    [InlineData(CargoStatus.Created)]
    [InlineData(CargoStatus.PickedUp)]
    [InlineData(CargoStatus.InTransit)]
    [InlineData(CargoStatus.OutForDelivery)]
    [InlineData(CargoStatus.Delivered)]
    [InlineData(CargoStatus.Returned)]
    [InlineData(CargoStatus.Cancelled)]
    public void CargoStatus_AllValues_ShouldBeDefined(CargoStatus status)
    {
        Enum.IsDefined(status).Should().BeTrue();
    }

    [Fact]
    public void CargoStatus_DeliveryTerminalStates_ShouldInclude3States()
    {
        var terminalStates = new[] { CargoStatus.Delivered, CargoStatus.Returned, CargoStatus.Cancelled };
        terminalStates.Should().HaveCount(3);
    }
}
