using FluentAssertions;
using MesTech.Application.DTOs.Cargo;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Cargo;
using MesTech.Domain.Enums;
using MesTech.Domain.ValueObjects;
using MesTech.Infrastructure.Integration.Factory;
using MesTech.Tests.Unit._Shared;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Infrastructure;

/// <summary>
/// CargoProviderSelector v2 testleri — Phase C: 3 strateji destegi.
/// AvailabilityFirst, CheapestFirst, FastestFirst + fallback senaryolari.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "CargoProviderSelector")]
[Trait("Phase", "PhaseC")]
public class CargoProviderSelectorV2Tests
{
    private readonly Mock<ICargoProviderFactory> _factoryMock;
    private readonly Mock<ILogger<CargoProviderSelector>> _loggerMock;
    private readonly CargoProviderSelector _sut;

    public CargoProviderSelectorV2Tests()
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

    /// <summary>
    /// Creates an adapter mock that also implements ICargoRateProvider.
    /// </summary>
    private Mock<T> CreateRateAdapterMock<T>(
        CargoProvider provider,
        bool isAvailable,
        decimal price,
        TimeSpan eta)
        where T : class, ICargoAdapter, ICargoRateProvider
    {
        var mock = new Mock<T>();
        mock.Setup(a => a.Provider).Returns(provider);
        mock.Setup(a => a.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(isAvailable);
        mock.Setup(a => a.GetRateAsync(It.IsAny<ShipmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CargoRateResult(provider, price, "TRY", eta, true));
        return mock;
    }

    private void SetupFactory(CargoProvider provider, ICargoAdapter? adapter)
    {
        _factoryMock
            .Setup(f => f.Resolve(provider))
            .Returns(adapter);
    }

    private static ShipmentRequest CreateShipmentRequest()
    {
        return new ShipmentRequest
        {
            OrderId = Guid.NewGuid(),
            RecipientName = "Test Customer",
            RecipientPhone = "05551234567",
            RecipientAddress = new Address { City = "Istanbul", District = "Kadikoy", Street = "Test Sk.", PostalCode = "34710" },
            SenderAddress = new Address { City = "Istanbul", District = "Besiktas", Street = "Depo Sk.", PostalCode = "34300" },
            Weight = 2.5m,
            Desi = 3,
            ParcelCount = 1
        };
    }

    // ===========================================================================
    // TEST 1: AvailabilityFirst_ReturnsFirstAvailable
    // ===========================================================================

    [Fact]
    public async Task AvailabilityFirst_ReturnsFirstAvailable()
    {
        // Arrange — Yurtici unavailable, Aras available
        var yurtici = CreateAdapterMock(CargoProvider.YurticiKargo, isAvailable: false);
        var aras = CreateAdapterMock(CargoProvider.ArasKargo, isAvailable: true);
        var surat = CreateAdapterMock(CargoProvider.SuratKargo, isAvailable: true);

        SetupFactory(CargoProvider.YurticiKargo, yurtici.Object);
        SetupFactory(CargoProvider.ArasKargo, aras.Object);
        SetupFactory(CargoProvider.SuratKargo, surat.Object);

        var order = FakeData.CreateOrder();

        // Act
        var result = await _sut.SelectBestProviderAsync(
            order, CargoSelectionStrategy.AvailabilityFirst);

        // Assert
        result.Should().Be(CargoProvider.ArasKargo);
    }

    // ===========================================================================
    // TEST 2: CheapestFirst_ReturnsCheapest
    // ===========================================================================

    [Fact]
    public async Task CheapestFirst_ReturnsCheapest()
    {
        // Arrange — 3 providers with different prices
        var yurtici = CreateRateAdapterMock<IRateCargoAdapter>(
            CargoProvider.YurticiKargo, true, price: 50m, eta: TimeSpan.FromDays(3));
        var aras = CreateRateAdapterMock<IRateCargoAdapter>(
            CargoProvider.ArasKargo, true, price: 35m, eta: TimeSpan.FromDays(2));
        var surat = CreateRateAdapterMock<IRateCargoAdapter>(
            CargoProvider.SuratKargo, true, price: 45m, eta: TimeSpan.FromDays(1));

        _factoryMock.Setup(f => f.GetAll()).Returns(new List<ICargoAdapter>
        {
            yurtici.Object,
            aras.Object,
            surat.Object
        }.AsReadOnly());

        var order = FakeData.CreateOrder();
        var request = CreateShipmentRequest();

        // Act
        var result = await _sut.SelectBestProviderAsync(
            order, CargoSelectionStrategy.CheapestFirst, request);

        // Assert — Aras has price=35, cheapest
        result.Should().Be(CargoProvider.ArasKargo);
    }

    // ===========================================================================
    // TEST 3: FastestFirst_ReturnsFastest
    // ===========================================================================

    [Fact]
    public async Task FastestFirst_ReturnsFastest()
    {
        // Arrange — 3 providers with different ETAs
        var yurtici = CreateRateAdapterMock<IRateCargoAdapter>(
            CargoProvider.YurticiKargo, true, price: 50m, eta: TimeSpan.FromDays(3));
        var aras = CreateRateAdapterMock<IRateCargoAdapter>(
            CargoProvider.ArasKargo, true, price: 35m, eta: TimeSpan.FromDays(2));
        var surat = CreateRateAdapterMock<IRateCargoAdapter>(
            CargoProvider.SuratKargo, true, price: 65m, eta: TimeSpan.FromHours(18));

        _factoryMock.Setup(f => f.GetAll()).Returns(new List<ICargoAdapter>
        {
            yurtici.Object,
            aras.Object,
            surat.Object
        }.AsReadOnly());

        var order = FakeData.CreateOrder();
        var request = CreateShipmentRequest();

        // Act
        var result = await _sut.SelectBestProviderAsync(
            order, CargoSelectionStrategy.FastestFirst, request);

        // Assert — Surat has 18h ETA, fastest
        result.Should().Be(CargoProvider.SuratKargo);
    }

    // ===========================================================================
    // TEST 4: CheapestFirst_NoRateProviders_FallsBackToAvailability
    // ===========================================================================

    [Fact]
    public async Task CheapestFirst_NoRateProviders_FallsBackToAvailability()
    {
        // Arrange — All adapters are plain ICargoAdapter (no ICargoRateProvider)
        var yurtici = CreateAdapterMock(CargoProvider.YurticiKargo, isAvailable: false);
        var aras = CreateAdapterMock(CargoProvider.ArasKargo, isAvailable: true);
        var surat = CreateAdapterMock(CargoProvider.SuratKargo, isAvailable: true);

        _factoryMock.Setup(f => f.GetAll()).Returns(new List<ICargoAdapter>
        {
            yurtici.Object,
            aras.Object,
            surat.Object
        }.AsReadOnly());

        // Also setup Resolve for AvailabilityFirst fallback
        SetupFactory(CargoProvider.YurticiKargo, yurtici.Object);
        SetupFactory(CargoProvider.ArasKargo, aras.Object);
        SetupFactory(CargoProvider.SuratKargo, surat.Object);

        var order = FakeData.CreateOrder();
        var request = CreateShipmentRequest();

        // Act
        var result = await _sut.SelectBestProviderAsync(
            order, CargoSelectionStrategy.CheapestFirst, request);

        // Assert — Falls back to AvailabilityFirst: Yurtici unavailable → Aras
        result.Should().Be(CargoProvider.ArasKargo);
    }

    // ===========================================================================
    // TEST 5: AllUnavailable_ReturnsDefault
    // ===========================================================================

    [Fact]
    public async Task AllUnavailable_ReturnsDefault()
    {
        // Arrange — All providers unavailable
        var yurtici = CreateRateAdapterMock<IRateCargoAdapter>(
            CargoProvider.YurticiKargo, false, 50m, TimeSpan.FromDays(3));
        var aras = CreateRateAdapterMock<IRateCargoAdapter>(
            CargoProvider.ArasKargo, false, 35m, TimeSpan.FromDays(2));
        var surat = CreateRateAdapterMock<IRateCargoAdapter>(
            CargoProvider.SuratKargo, false, 65m, TimeSpan.FromDays(1));

        _factoryMock.Setup(f => f.GetAll()).Returns(new List<ICargoAdapter>
        {
            yurtici.Object,
            aras.Object,
            surat.Object
        }.AsReadOnly());

        // Fallback also needs Resolve setup for AvailabilityFirst
        SetupFactory(CargoProvider.YurticiKargo, yurtici.Object);
        SetupFactory(CargoProvider.ArasKargo, aras.Object);
        SetupFactory(CargoProvider.SuratKargo, surat.Object);

        // Make IsAvailableAsync return false for all
        yurtici.Setup(a => a.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        aras.Setup(a => a.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        surat.Setup(a => a.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var order = FakeData.CreateOrder();
        var request = CreateShipmentRequest();

        // Act — CheapestFirst, but all unavailable → fallback → still all unavailable → default
        var result = await _sut.SelectBestProviderAsync(
            order, CargoSelectionStrategy.CheapestFirst, request);

        // Assert — Default fallback is YurticiKargo
        result.Should().Be(CargoProvider.YurticiKargo);
    }

    // ===========================================================================
    // TEST 6: Default overload uses AvailabilityFirst
    // ===========================================================================

    [Fact]
    public async Task DefaultOverload_UsesAvailabilityFirst()
    {
        // Arrange
        var yurtici = CreateAdapterMock(CargoProvider.YurticiKargo, isAvailable: true);
        SetupFactory(CargoProvider.YurticiKargo, yurtici.Object);
        SetupFactory(CargoProvider.ArasKargo, null);
        SetupFactory(CargoProvider.SuratKargo, null);

        var order = FakeData.CreateOrder();

        // Act — no strategy parameter
        var result = await _sut.SelectBestProviderAsync(order);

        // Assert
        result.Should().Be(CargoProvider.YurticiKargo);
    }

    // ===========================================================================
    // TEST 7: NullOrder_ThrowsArgumentNullException
    // ===========================================================================

    [Fact]
    public async Task NullOrder_ThrowsArgumentNullException()
    {
        var act = () => _sut.SelectBestProviderAsync(null!, CargoSelectionStrategy.CheapestFirst);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ===========================================================================
    // TEST 8: RateQueryThrows_SkipsAndContinues
    // ===========================================================================

    [Fact]
    public async Task CheapestFirst_RateQueryThrows_SkipsAndContinues()
    {
        // Arrange — Yurtici rate query throws, Aras returns rate
        var yurtici = new Mock<IRateCargoAdapter>();
        yurtici.Setup(a => a.Provider).Returns(CargoProvider.YurticiKargo);
        yurtici.Setup(a => a.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        yurtici.Setup(a => a.GetRateAsync(It.IsAny<ShipmentRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("API unreachable"));

        var aras = CreateRateAdapterMock<IRateCargoAdapter>(
            CargoProvider.ArasKargo, true, price: 40m, eta: TimeSpan.FromDays(2));

        _factoryMock.Setup(f => f.GetAll()).Returns(new List<ICargoAdapter>
        {
            yurtici.Object,
            aras.Object
        }.AsReadOnly());

        var order = FakeData.CreateOrder();
        var request = CreateShipmentRequest();

        // Act
        var result = await _sut.SelectBestProviderAsync(
            order, CargoSelectionStrategy.CheapestFirst, request);

        // Assert — Yurtici skipped, Aras returned
        result.Should().Be(CargoProvider.ArasKargo);
    }
}

/// <summary>
/// Combined interface for testing — ICargoAdapter + ICargoRateProvider.
/// Moq needs a single interface target to implement both.
/// </summary>
public interface IRateCargoAdapter : ICargoAdapter, ICargoRateProvider { }
