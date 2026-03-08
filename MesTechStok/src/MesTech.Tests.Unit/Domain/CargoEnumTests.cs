using FluentAssertions;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain;

[Trait("Category", "Unit")]
public class CargoEnumTests
{
    [Theory]
    [InlineData(CargoProvider.YurticiKargo, 1)]
    [InlineData(CargoProvider.ArasKargo, 2)]
    [InlineData(CargoProvider.SuratKargo, 3)]
    [InlineData(CargoProvider.None, 0)]
    public void CargoProvider_ShouldHaveCorrectValues(CargoProvider provider, int expected)
    {
        ((int)provider).Should().Be(expected);
    }

    [Fact]
    public void CargoProvider_ShouldHave8Members()
    {
        Enum.GetValues<CargoProvider>().Should().HaveCount(8);
    }

    [Theory]
    [InlineData(CargoStatus.Created, 0)]
    [InlineData(CargoStatus.Delivered, 4)]
    [InlineData(CargoStatus.AtBranch, 8)]
    public void CargoStatus_ShouldHaveCorrectValues(CargoStatus status, int expected)
    {
        ((int)status).Should().Be(expected);
    }

    [Fact]
    public void CargoStatus_ShouldHave9Members()
    {
        Enum.GetValues<CargoStatus>().Should().HaveCount(9);
    }
}
