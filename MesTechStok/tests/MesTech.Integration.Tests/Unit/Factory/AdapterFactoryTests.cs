using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Factory;
using MesTech.Integration.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Factory;

public class AdapterFactoryTests
{
    private readonly Mock<ILogger<AdapterFactory>> _loggerMock = new();

    private AdapterFactory CreateFactory(params IIntegratorAdapter[] adapters)
        => new(adapters, _loggerMock.Object);

    [Fact]
    public void Resolve_ByPlatformType_ReturnsCorrectAdapter()
    {
        // Arrange
        var trendyolAdapter = new TestAdapterBuilder()
            .WithPlatformCode("Trendyol")
            .BuildObject();

        var factory = CreateFactory(trendyolAdapter);

        // Act
        var result = factory.Resolve(PlatformType.Trendyol);

        // Assert
        result.Should().NotBeNull();
        result!.PlatformCode.Should().Be("Trendyol");
    }

    [Theory]
    [InlineData("trendyol")]
    [InlineData("TRENDYOL")]
    [InlineData("Trendyol")]
    public void Resolve_ByString_CaseInsensitive(string code)
    {
        // Arrange
        var trendyolAdapter = new TestAdapterBuilder()
            .WithPlatformCode("Trendyol")
            .BuildObject();

        var factory = CreateFactory(trendyolAdapter);

        // Act
        var result = factory.Resolve(code);

        // Assert
        result.Should().NotBeNull();
        result!.PlatformCode.Should().Be("Trendyol");
    }

    [Fact]
    public void Resolve_UnknownPlatform_ReturnsNull()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        var result = factory.Resolve("NonExistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetAll_ReturnsAllRegistered()
    {
        // Arrange
        var adapter1 = new TestAdapterBuilder()
            .WithPlatformCode("Trendyol")
            .BuildObject();
        var adapter2 = new TestAdapterBuilder()
            .WithPlatformCode("OpenCart")
            .BuildObject();
        var adapter3 = new TestAdapterBuilder()
            .WithPlatformCode("N11")
            .BuildObject();

        var factory = CreateFactory(adapter1, adapter2, adapter3);

        // Act
        var all = factory.GetAll();

        // Assert
        all.Count.Should().Be(3);
        all.Should().Contain(a => a.PlatformCode == "Trendyol");
        all.Should().Contain(a => a.PlatformCode == "OpenCart");
        all.Should().Contain(a => a.PlatformCode == "N11");
    }

    [Fact]
    public void ResolveCapability_OrderCapable_ReturnsAdapter()
    {
        // Arrange — mock implements both IIntegratorAdapter AND IOrderCapableAdapter
        var mock = new Mock<IIntegratorAdapter>();
        mock.Setup(a => a.PlatformCode).Returns("Trendyol");
        mock.Setup(a => a.SupportsStockUpdate).Returns(true);
        mock.Setup(a => a.SupportsPriceUpdate).Returns(true);
        mock.Setup(a => a.SupportsShipment).Returns(false);

        var orderMock = mock.As<IOrderCapableAdapter>();
        orderMock.Setup(o => o.PullOrdersAsync(It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExternalOrderDto>());

        var factory = CreateFactory(mock.Object);

        // Act
        var result = factory.ResolveCapability<IOrderCapableAdapter>("Trendyol");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IOrderCapableAdapter>();
    }

    [Fact]
    public void ResolveCapability_Unsupported_ReturnsNull()
    {
        // Arrange — adapter does NOT implement ISettlementCapableAdapter
        var adapter = new TestAdapterBuilder()
            .WithPlatformCode("OpenCart")
            .BuildObject();

        var factory = CreateFactory(adapter);

        // Act
        var result = factory.ResolveCapability<ISettlementCapableAdapter>("OpenCart");

        // Assert
        result.Should().BeNull();
    }
}
