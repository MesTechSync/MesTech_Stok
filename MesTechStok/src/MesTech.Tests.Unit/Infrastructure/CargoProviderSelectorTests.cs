using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Factory;
using MesTech.Tests.Unit._Shared;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Infrastructure;

/// <summary>
/// CargoProviderSelector testleri.
/// Oncelik sirasi (Yurtici > Aras > Surat), musaitlik kontrolu,
/// hata toleransi ve fallback davranisi dogrulanir.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "CargoSelector")]
[Trait("Phase", "Dalga3")]
public class CargoProviderSelectorTests
{
    private readonly Mock<ICargoProviderFactory> _factoryMock;
    private readonly Mock<ILogger<CargoProviderSelector>> _loggerMock;
    private readonly CargoProviderSelector _sut;

    public CargoProviderSelectorTests()
    {
        _factoryMock = new Mock<ICargoProviderFactory>();
        _loggerMock = new Mock<ILogger<CargoProviderSelector>>();
        _sut = new CargoProviderSelector(_factoryMock.Object, _loggerMock.Object);
    }

    // -- Helpers ----------------------------------------------------------------

    private Mock<ICargoAdapter> CreateAdapterMock(CargoProvider provider, bool isAvailable)
    {
        var mock = new Mock<ICargoAdapter>();
        mock.Setup(a => a.Provider).Returns(provider);
        mock.Setup(a => a.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(isAvailable);
        return mock;
    }

    private void SetupFactory(CargoProvider provider, ICargoAdapter? adapter)
    {
        _factoryMock
            .Setup(f => f.Resolve(provider))
            .Returns(adapter);
    }

    // ===========================================================================
    // 1. All available -> returns first priority (YurticiKargo)
    // ===========================================================================

    [Fact]
    public async Task SelectBestProviderAsync_FirstAvailable_ReturnsYurticiKargo()
    {
        // Arrange
        var yurtici = CreateAdapterMock(CargoProvider.YurticiKargo, isAvailable: true);
        var aras = CreateAdapterMock(CargoProvider.ArasKargo, isAvailable: true);
        var surat = CreateAdapterMock(CargoProvider.SuratKargo, isAvailable: true);

        SetupFactory(CargoProvider.YurticiKargo, yurtici.Object);
        SetupFactory(CargoProvider.ArasKargo, aras.Object);
        SetupFactory(CargoProvider.SuratKargo, surat.Object);

        var order = FakeData.CreateOrder();

        // Act
        var result = await _sut.SelectBestProviderAsync(order);

        // Assert
        result.Should().Be(CargoProvider.YurticiKargo);

        yurtici.Verify(a => a.IsAvailableAsync(It.IsAny<CancellationToken>()), Times.Once);
        aras.Verify(a => a.IsAvailableAsync(It.IsAny<CancellationToken>()), Times.Never,
            "Should not check lower-priority adapters once a match is found");
        surat.Verify(a => a.IsAvailableAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ===========================================================================
    // 2. First unavailable -> returns fallback (ArasKargo)
    // ===========================================================================

    [Fact]
    public async Task SelectBestProviderAsync_FirstUnavailable_ReturnsFallback()
    {
        // Arrange
        var yurtici = CreateAdapterMock(CargoProvider.YurticiKargo, isAvailable: false);
        var aras = CreateAdapterMock(CargoProvider.ArasKargo, isAvailable: true);
        var surat = CreateAdapterMock(CargoProvider.SuratKargo, isAvailable: true);

        SetupFactory(CargoProvider.YurticiKargo, yurtici.Object);
        SetupFactory(CargoProvider.ArasKargo, aras.Object);
        SetupFactory(CargoProvider.SuratKargo, surat.Object);

        var order = FakeData.CreateOrder();

        // Act
        var result = await _sut.SelectBestProviderAsync(order);

        // Assert
        result.Should().Be(CargoProvider.ArasKargo);
    }

    // ===========================================================================
    // 3. First two unavailable -> returns third (SuratKargo)
    // ===========================================================================

    [Fact]
    public async Task SelectBestProviderAsync_FirstTwoUnavailable_ReturnsThird()
    {
        // Arrange
        var yurtici = CreateAdapterMock(CargoProvider.YurticiKargo, isAvailable: false);
        var aras = CreateAdapterMock(CargoProvider.ArasKargo, isAvailable: false);
        var surat = CreateAdapterMock(CargoProvider.SuratKargo, isAvailable: true);

        SetupFactory(CargoProvider.YurticiKargo, yurtici.Object);
        SetupFactory(CargoProvider.ArasKargo, aras.Object);
        SetupFactory(CargoProvider.SuratKargo, surat.Object);

        var order = FakeData.CreateOrder();

        // Act
        var result = await _sut.SelectBestProviderAsync(order);

        // Assert
        result.Should().Be(CargoProvider.SuratKargo);
    }

    // ===========================================================================
    // 4. All unavailable -> defaults to YurticiKargo
    // ===========================================================================

    [Fact]
    public async Task SelectBestProviderAsync_AllUnavailable_DefaultsToYurticiKargo()
    {
        // Arrange
        var yurtici = CreateAdapterMock(CargoProvider.YurticiKargo, isAvailable: false);
        var aras = CreateAdapterMock(CargoProvider.ArasKargo, isAvailable: false);
        var surat = CreateAdapterMock(CargoProvider.SuratKargo, isAvailable: false);

        SetupFactory(CargoProvider.YurticiKargo, yurtici.Object);
        SetupFactory(CargoProvider.ArasKargo, aras.Object);
        SetupFactory(CargoProvider.SuratKargo, surat.Object);

        var order = FakeData.CreateOrder();

        // Act
        var result = await _sut.SelectBestProviderAsync(order);

        // Assert
        result.Should().Be(CargoProvider.YurticiKargo,
            "When no provider is available, default fallback must be YurticiKargo");
    }

    // ===========================================================================
    // 5. Adapter not resolved (null) -> skips provider
    // ===========================================================================

    [Fact]
    public async Task SelectBestProviderAsync_AdapterNotResolved_SkipsProvider()
    {
        // Arrange — factory returns null for YurticiKargo (not registered)
        SetupFactory(CargoProvider.YurticiKargo, null);

        var aras = CreateAdapterMock(CargoProvider.ArasKargo, isAvailable: true);
        SetupFactory(CargoProvider.ArasKargo, aras.Object);
        SetupFactory(CargoProvider.SuratKargo, null);

        var order = FakeData.CreateOrder();

        // Act
        var result = await _sut.SelectBestProviderAsync(order);

        // Assert
        result.Should().Be(CargoProvider.ArasKargo,
            "Null adapter from factory should be skipped, falling through to next provider");
    }

    // ===========================================================================
    // 6. Availability check throws -> continues to next
    // ===========================================================================

    [Fact]
    public async Task SelectBestProviderAsync_AvailabilityCheckThrows_ContinuesToNext()
    {
        // Arrange — YurticiKargo availability check throws
        var yurtici = new Mock<ICargoAdapter>();
        yurtici.Setup(a => a.Provider).Returns(CargoProvider.YurticiKargo);
        yurtici.Setup(a => a.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("SOAP endpoint unreachable"));

        var aras = CreateAdapterMock(CargoProvider.ArasKargo, isAvailable: true);

        SetupFactory(CargoProvider.YurticiKargo, yurtici.Object);
        SetupFactory(CargoProvider.ArasKargo, aras.Object);
        SetupFactory(CargoProvider.SuratKargo, null);

        var order = FakeData.CreateOrder();

        // Act
        var result = await _sut.SelectBestProviderAsync(order);

        // Assert
        result.Should().Be(CargoProvider.ArasKargo,
            "Exception in availability check must be swallowed; next provider should be tried");
    }

    // ===========================================================================
    // 7. Null order -> throws ArgumentNullException
    // ===========================================================================

    [Fact]
    public async Task SelectBestProviderAsync_NullOrder_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.SelectBestProviderAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ===========================================================================
    // 8. Constructor null dependencies -> throws ArgumentNullException
    // ===========================================================================

    [Theory]
    [InlineData("factory")]
    [InlineData("logger")]
    public void Constructor_NullDependencies_ThrowsArgumentNullException(string nullParam)
    {
        Action action = nullParam switch
        {
            "factory" => () => new CargoProviderSelector(null!, _loggerMock.Object),
            "logger"  => () => new CargoProviderSelector(_factoryMock.Object, null!),
            _         => throw new ArgumentOutOfRangeException(nameof(nullParam))
        };

        action.Should().Throw<ArgumentNullException>().WithParameterName(nullParam);
    }
}
