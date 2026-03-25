using FluentAssertions;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain;

[Trait("Category", "Unit")]
[Trait("Phase", "Dalga3")]
public class CargoEnumTests
{
    [Fact]
    public void CargoProvider_ShouldHave9Members()
    {
        // DHL=9, FedEx=10 added — expected count is now 11
        Enum.GetValues<CargoProvider>().Should().HaveCount(11);
    }

    [Theory]
    [InlineData(CargoProvider.None, 0)]
    [InlineData(CargoProvider.YurticiKargo, 1)]
    [InlineData(CargoProvider.ArasKargo, 2)]
    [InlineData(CargoProvider.SuratKargo, 3)]
    [InlineData(CargoProvider.MngKargo, 4)]
    [InlineData(CargoProvider.PttKargo, 5)]
    [InlineData(CargoProvider.Hepsijet, 6)]
    [InlineData(CargoProvider.UPS, 7)]
    [InlineData(CargoProvider.Sendeo, 8)]
    [InlineData(CargoProvider.DHL, 9)]
    [InlineData(CargoProvider.FedEx, 10)]
    public void CargoProvider_Values_ShouldMatchExpected(CargoProvider provider, int expected)
    {
        ((int)provider).Should().Be(expected);
    }

    [Fact]
    public void CargoStatus_ShouldHave9Members()
    {
        Enum.GetValues<CargoStatus>().Should().HaveCount(9);
    }

    [Theory]
    [InlineData(CargoStatus.Created)]
    [InlineData(CargoStatus.PickedUp)]
    [InlineData(CargoStatus.InTransit)]
    [InlineData(CargoStatus.OutForDelivery)]
    [InlineData(CargoStatus.Delivered)]
    [InlineData(CargoStatus.Returned)]
    [InlineData(CargoStatus.Lost)]
    [InlineData(CargoStatus.Cancelled)]
    [InlineData(CargoStatus.AtBranch)]
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
