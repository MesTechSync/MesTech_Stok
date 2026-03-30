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

    public GetCargoProvidersHandlerTests()
    {
        _sut = new GetCargoProvidersHandler(_factoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithAdapters_ReturnsProviderList()
    {
        var adapter = CreateMockAdapter(CargoProvider.YurticiKargo, isAvailable: true);
        _factoryMock.Setup(f => f.GetAll()).Returns(new List<ICargoAdapter> { adapter.Object });

        var result = await _sut.Handle(new GetCargoProvidersQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("YurticiKargo");
        result[0].IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NoAdapters_ReturnsEmptyList()
    {
        _factoryMock.Setup(f => f.GetAll()).Returns(new List<ICargoAdapter>());

        var result = await _sut.Handle(new GetCargoProvidersQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_AdapterThrows_SetsInactive()
    {
        var adapter = new Mock<ICargoAdapter>();
        adapter.Setup(a => a.Provider).Returns(CargoProvider.ArasKargo);
        adapter.Setup(a => a.IsAvailableAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("timeout"));

        _factoryMock.Setup(f => f.GetAll()).Returns(new List<ICargoAdapter> { adapter.Object });

        var result = await _sut.Handle(new GetCargoProvidersQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithFeatures_BuildsContractInfo()
    {
        var adapter = CreateMockAdapter(CargoProvider.YurticiKargo, isAvailable: true,
            supportsCod: true, supportsLabel: true);
        _factoryMock.Setup(f => f.GetAll()).Returns(new List<ICargoAdapter> { adapter.Object });

        var result = await _sut.Handle(new GetCargoProvidersQuery(Guid.NewGuid()), CancellationToken.None);

        result[0].ContractInfo.Should().Contain("COD");
        result[0].ContractInfo.Should().Contain("Label");
    }

    private static Mock<ICargoAdapter> CreateMockAdapter(
        CargoProvider provider, bool isAvailable,
        bool supportsCod = false, bool supportsLabel = false)
    {
        var mock = new Mock<ICargoAdapter>();
        mock.Setup(a => a.Provider).Returns(provider);
        mock.Setup(a => a.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(isAvailable);
        mock.Setup(a => a.SupportsCashOnDelivery).Returns(supportsCod);
        mock.Setup(a => a.SupportsLabelGeneration).Returns(supportsLabel);
        return mock;
    }
}
