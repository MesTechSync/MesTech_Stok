using FluentAssertions;
using MesTech.Application.Features.Cargo.Queries.GetCargoProviders;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetCargoProvidersHandlerTests
{
    private readonly Mock<ICargoProviderFactory> _factoryMock = new();
    private readonly Mock<ILogger<GetCargoProvidersHandler>> _loggerMock = new();
    private readonly GetCargoProvidersHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetCargoProvidersHandlerTests()
    {
        _sut = new GetCargoProvidersHandler(_factoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_NoAdapters_ReturnsEmptyList()
    {
        // Arrange
        _factoryMock.Setup(f => f.GetAll()).Returns(new List<ICargoAdapter>());
        var query = new GetCargoProvidersQuery(_tenantId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_SingleAvailableAdapter_ReturnsOneActiveProvider()
    {
        // Arrange
        var adapterMock = new Mock<ICargoAdapter>();
        adapterMock.Setup(a => a.Provider).Returns(CargoProvider.YurticiKargo);
        adapterMock.Setup(a => a.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        adapterMock.Setup(a => a.SupportsCashOnDelivery).Returns(true);
        adapterMock.Setup(a => a.SupportsMultiParcel).Returns(false);
        adapterMock.Setup(a => a.SupportsLabelGeneration).Returns(true);
        adapterMock.Setup(a => a.SupportsCancellation).Returns(false);

        _factoryMock.Setup(f => f.GetAll()).Returns(new List<ICargoAdapter> { adapterMock.Object });
        var query = new GetCargoProvidersQuery(_tenantId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        var dto = result[0];
        dto.Name.Should().Be("YurticiKargo");
        dto.Code.Should().Be("YurticiKargo");
        dto.IsActive.Should().BeTrue();
        dto.ContractInfo.Should().Contain("COD");
        dto.ContractInfo.Should().Contain("Label");
        dto.ContractInfo.Should().NotContain("MultiParcel");
        dto.AvgDeliveryDays.Should().Be(2);
    }

    [Fact]
    public async Task Handle_AdapterThrowsException_ReturnsInactiveProvider()
    {
        // Arrange
        var adapterMock = new Mock<ICargoAdapter>();
        adapterMock.Setup(a => a.Provider).Returns(CargoProvider.ArasKargo);
        adapterMock.Setup(a => a.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));
        adapterMock.Setup(a => a.SupportsCashOnDelivery).Returns(false);
        adapterMock.Setup(a => a.SupportsMultiParcel).Returns(false);
        adapterMock.Setup(a => a.SupportsLabelGeneration).Returns(false);
        adapterMock.Setup(a => a.SupportsCancellation).Returns(false);

        _factoryMock.Setup(f => f.GetAll()).Returns(new List<ICargoAdapter> { adapterMock.Object });
        var query = new GetCargoProvidersQuery(_tenantId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].IsActive.Should().BeFalse();
        result[0].Name.Should().Be("ArasKargo");
    }

    [Fact]
    public async Task Handle_MultipleAdapters_ReturnsAllWithCorrectDeliveryDays()
    {
        // Arrange
        var hepsijet = CreateAdapterMock(CargoProvider.Hepsijet, true);
        var ptt = CreateAdapterMock(CargoProvider.PttKargo, true);
        var ups = CreateAdapterMock(CargoProvider.UPS, false);

        _factoryMock.Setup(f => f.GetAll())
            .Returns(new List<ICargoAdapter> { hepsijet.Object, ptt.Object, ups.Object });
        var query = new GetCargoProvidersQuery(_tenantId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result[0].AvgDeliveryDays.Should().Be(1); // Hepsijet
        result[1].AvgDeliveryDays.Should().Be(4); // PttKargo
        result[2].AvgDeliveryDays.Should().Be(3); // UPS
        result[2].IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_AdapterWithAllFeatures_ContractInfoContainsAll()
    {
        // Arrange
        var adapterMock = new Mock<ICargoAdapter>();
        adapterMock.Setup(a => a.Provider).Returns(CargoProvider.DHL);
        adapterMock.Setup(a => a.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        adapterMock.Setup(a => a.SupportsCashOnDelivery).Returns(true);
        adapterMock.Setup(a => a.SupportsMultiParcel).Returns(true);
        adapterMock.Setup(a => a.SupportsLabelGeneration).Returns(true);
        adapterMock.Setup(a => a.SupportsCancellation).Returns(true);

        _factoryMock.Setup(f => f.GetAll()).Returns(new List<ICargoAdapter> { adapterMock.Object });
        var query = new GetCargoProvidersQuery(_tenantId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].ContractInfo.Should().Be("COD, MultiParcel, Label, Cancel");
    }

    private static Mock<ICargoAdapter> CreateAdapterMock(CargoProvider provider, bool available)
    {
        var mock = new Mock<ICargoAdapter>();
        mock.Setup(a => a.Provider).Returns(provider);
        mock.Setup(a => a.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(available);
        mock.Setup(a => a.SupportsCashOnDelivery).Returns(false);
        mock.Setup(a => a.SupportsMultiParcel).Returns(false);
        mock.Setup(a => a.SupportsLabelGeneration).Returns(false);
        mock.Setup(a => a.SupportsCancellation).Returns(false);
        return mock;
    }
}
