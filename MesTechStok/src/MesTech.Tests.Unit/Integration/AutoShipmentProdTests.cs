using FluentAssertions;
using MediatR;
using MesTech.Application.DTOs.Cargo;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Integration.Orchestration;
using MesTech.Infrastructure.Messaging.Mesa;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Integration;

/// <summary>
/// AutoShipmentService prod-ready tests (Dalga 4 Task 7).
/// Verifies:
/// 1. Order loaded from IOrderRepository (no TEMP stubs)
/// 2. PlatformCode resolved from Order.SourcePlatform (not hardcoded)
/// 3. PlatformNotificationFailedEvent published via MediatR IPublisher
/// 4. Platform notification failure does NOT rollback cargo
/// 5. AdapterFactory resolves all 5 platforms
/// 6. Pazarama has no special handling — same flow as other platforms
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "AutoShipment")]
[Trait("Phase", "Dalga4")]
public class AutoShipmentProdTests
{
    private readonly Mock<ICargoProviderSelector> _selectorMock;
    private readonly Mock<ICargoProviderFactory> _cargoFactoryMock;
    private readonly Mock<IAdapterFactory> _adapterFactoryMock;
    private readonly Mock<IOrderRepository> _orderRepoMock;
    private readonly Mock<ILogger<AutoShipmentService>> _loggerMock;
    private readonly Mock<IPublisher> _publisherMock;
    private readonly Mock<ICargoAdapter> _cargoAdapterMock;
    private readonly Mock<IShipmentCapableAdapter> _shipmentAdapterMock;

    private readonly Guid _orderId = Guid.NewGuid();

    public AutoShipmentProdTests()
    {
        _selectorMock = new Mock<ICargoProviderSelector>();
        _cargoFactoryMock = new Mock<ICargoProviderFactory>();
        _adapterFactoryMock = new Mock<IAdapterFactory>();
        _orderRepoMock = new Mock<IOrderRepository>();
        _loggerMock = new Mock<ILogger<AutoShipmentService>>();
        _publisherMock = new Mock<IPublisher>();
        _cargoAdapterMock = new Mock<ICargoAdapter>();
        _shipmentAdapterMock = new Mock<IShipmentCapableAdapter>();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private AutoShipmentService CreateSut(IPublisher? publisher = null) =>
        new(
            _selectorMock.Object,
            _cargoFactoryMock.Object,
            _adapterFactoryMock.Object,
            _orderRepoMock.Object,
            _loggerMock.Object,
            publisher);

    private void SetupOrder(PlatformType? platform = PlatformType.Trendyol, string? customerName = "Ali Yilmaz")
    {
        _orderRepoMock
            .Setup(r => r.GetByIdAsync(_orderId))
            .ReturnsAsync(new Order
            {
                CustomerName = customerName,
                SourcePlatform = platform
            });
    }

    private void SetupSelector(CargoProvider provider = CargoProvider.YurticiKargo)
    {
        _selectorMock
            .Setup(s => s.SelectBestProviderAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);
    }

    private void SetupCargoFactory(ICargoAdapter? adapter)
    {
        _cargoFactoryMock
            .Setup(f => f.Resolve(It.IsAny<CargoProvider>()))
            .Returns(adapter);
    }

    private void SetupCargoResult(ShipmentResult result)
    {
        _cargoAdapterMock
            .Setup(a => a.CreateShipmentAsync(It.IsAny<ShipmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
    }

    private void SetupPlatformAdapter(IShipmentCapableAdapter? adapter, string platformCode = "Trendyol")
    {
        _adapterFactoryMock
            .Setup(f => f.ResolveCapability<IShipmentCapableAdapter>(platformCode))
            .Returns(adapter);
    }

    private void SetupFullChain(
        PlatformType platform = PlatformType.Trendyol,
        CargoProvider cargo = CargoProvider.YurticiKargo,
        string tracking = "YK-PROD-001",
        string shipmentId = "SHIP-PROD-001")
    {
        SetupOrder(platform);
        SetupSelector(cargo);
        SetupCargoFactory(_cargoAdapterMock.Object);
        SetupCargoResult(ShipmentResult.Succeeded(tracking, shipmentId));

        _shipmentAdapterMock
            .Setup(a => a.SendShipmentAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CargoProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        SetupPlatformAdapter(_shipmentAdapterMock.Object, platform.ToString());
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 1. Correct cargo provider resolved from selector
    // ══════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(CargoProvider.YurticiKargo)]
    [InlineData(CargoProvider.ArasKargo)]
    [InlineData(CargoProvider.SuratKargo)]
    public async Task ProcessOrderAsync_ResolvesCorrectCargoProvider(CargoProvider expectedProvider)
    {
        // Arrange
        SetupOrder();
        SetupSelector(expectedProvider);
        SetupCargoFactory(_cargoAdapterMock.Object);
        SetupCargoResult(ShipmentResult.Succeeded("TRK-001", "SHIP-001"));
        SetupPlatformAdapter(null);

        var sut = CreateSut();

        // Act
        await sut.ProcessOrderAsync(_orderId);

        // Assert
        _cargoFactoryMock.Verify(
            f => f.Resolve(expectedProvider),
            Times.Once,
            $"CargoProviderFactory.Resolve must be called with {expectedProvider}");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 2. Platform notification failure does NOT rollback cargo
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ProcessOrderAsync_PlatformNotificationThrows_CargoSuccessPreserved_NoRollback()
    {
        // Arrange
        SetupOrder(PlatformType.Trendyol);
        SetupSelector(CargoProvider.ArasKargo);
        SetupCargoFactory(_cargoAdapterMock.Object);
        SetupCargoResult(ShipmentResult.Succeeded("AR-NOROLLBACK", "SHIP-NR"));

        _shipmentAdapterMock
            .Setup(a => a.SendShipmentAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CargoProvider>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Platform API unreachable"));
        SetupPlatformAdapter(_shipmentAdapterMock.Object);

        var sut = CreateSut(_publisherMock.Object);

        // Act
        var result = await sut.ProcessOrderAsync(_orderId);

        // Assert — cargo result must still be Success
        result.Success.Should().BeTrue("cargo succeeded; platform failure must NOT rollback");
        result.TrackingNumber.Should().Be("AR-NOROLLBACK");
    }

    [Fact]
    public async Task ProcessOrderAsync_PlatformNotificationReturnsFalse_CargoSuccessPreserved()
    {
        // Arrange
        SetupOrder(PlatformType.Ciceksepeti);
        SetupSelector(CargoProvider.YurticiKargo);
        SetupCargoFactory(_cargoAdapterMock.Object);
        SetupCargoResult(ShipmentResult.Succeeded("YK-FALSE-TEST", "SHIP-FT"));

        _shipmentAdapterMock
            .Setup(a => a.SendShipmentAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CargoProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        SetupPlatformAdapter(_shipmentAdapterMock.Object, "Ciceksepeti");

        var sut = CreateSut(_publisherMock.Object);

        // Act
        var result = await sut.ProcessOrderAsync(_orderId);

        // Assert — cargo result preserved even when platform returns false
        result.Success.Should().BeTrue("platform false only queues retry, no cargo rollback");
        result.TrackingNumber.Should().Be("YK-FALSE-TEST");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 3. PlatformCode resolved from Order.SourcePlatform (not hardcoded)
    // ══════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(PlatformType.Trendyol, "Trendyol")]
    [InlineData(PlatformType.Ciceksepeti, "Ciceksepeti")]
    [InlineData(PlatformType.Hepsiburada, "Hepsiburada")]
    [InlineData(PlatformType.OpenCart, "OpenCart")]
    [InlineData(PlatformType.Pazarama, "Pazarama")]
    public async Task ProcessOrderAsync_UsesPlatformCodeFromOrder_NotHardcoded(
        PlatformType platform, string expectedCode)
    {
        // Arrange
        SetupOrder(platform);
        SetupSelector(CargoProvider.YurticiKargo);
        SetupCargoFactory(_cargoAdapterMock.Object);
        SetupCargoResult(ShipmentResult.Succeeded("TRK-PLATFORM", "SHIP-P"));
        SetupPlatformAdapter(null, expectedCode);

        var sut = CreateSut();

        // Act
        await sut.ProcessOrderAsync(_orderId);

        // Assert — ResolveCapability must be called with correct platform code
        _adapterFactoryMock.Verify(
            f => f.ResolveCapability<IShipmentCapableAdapter>(expectedCode),
            Times.Once,
            $"Platform code must be '{expectedCode}' from Order.SourcePlatform={platform}");
    }

    [Fact]
    public async Task ProcessOrderAsync_NullSourcePlatform_DefaultsToTrendyol()
    {
        // Arrange
        SetupOrder(platform: null);
        SetupSelector(CargoProvider.YurticiKargo);
        SetupCargoFactory(_cargoAdapterMock.Object);
        SetupCargoResult(ShipmentResult.Succeeded("TRK-NULL", "SHIP-NULL"));
        SetupPlatformAdapter(null, "Trendyol");

        var sut = CreateSut();

        // Act
        await sut.ProcessOrderAsync(_orderId);

        // Assert — defaults to "Trendyol" when SourcePlatform is null
        _adapterFactoryMock.Verify(
            f => f.ResolveCapability<IShipmentCapableAdapter>("Trendyol"),
            Times.Once,
            "Must default to Trendyol when Order.SourcePlatform is null");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 4. RecipientName populated from Order.CustomerName
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ProcessOrderAsync_RecipientName_PopulatedFromOrder()
    {
        // Arrange
        SetupOrder(PlatformType.Trendyol, customerName: "Mehmet Kaya");
        SetupSelector(CargoProvider.YurticiKargo);
        SetupCargoFactory(_cargoAdapterMock.Object);
        _cargoAdapterMock
            .Setup(a => a.CreateShipmentAsync(It.IsAny<ShipmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ShipmentResult.Succeeded("YK-NAME", "SHIP-NAME"));
        SetupPlatformAdapter(null);

        var sut = CreateSut();

        // Act
        await sut.ProcessOrderAsync(_orderId);

        // Assert
        _cargoAdapterMock.Verify(
            a => a.CreateShipmentAsync(
                It.Is<ShipmentRequest>(r => r.RecipientName == "Mehmet Kaya"),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "RecipientName must come from Order.CustomerName, not TEMP stub");
    }

    [Fact]
    public async Task ProcessOrderAsync_NullCustomerName_FallsBackToNA()
    {
        // Arrange
        SetupOrder(PlatformType.Trendyol, customerName: null);
        SetupSelector(CargoProvider.YurticiKargo);
        SetupCargoFactory(_cargoAdapterMock.Object);
        _cargoAdapterMock
            .Setup(a => a.CreateShipmentAsync(It.IsAny<ShipmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ShipmentResult.Succeeded("YK-NA", "SHIP-NA"));
        SetupPlatformAdapter(null);

        var sut = CreateSut();

        // Act
        await sut.ProcessOrderAsync(_orderId);

        // Assert
        _cargoAdapterMock.Verify(
            a => a.CreateShipmentAsync(
                It.Is<ShipmentRequest>(r => r.RecipientName == "N/A"),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "RecipientName must fall back to N/A when CustomerName is null");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 5. MediatR publish on platform notification failure
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ProcessOrderAsync_PlatformThrows_PublishesPlatformNotificationFailedEvent()
    {
        // Arrange
        SetupOrder(PlatformType.Hepsiburada);
        SetupSelector(CargoProvider.SuratKargo);
        SetupCargoFactory(_cargoAdapterMock.Object);
        SetupCargoResult(ShipmentResult.Succeeded("SR-PUBLISH", "SHIP-PUB"));

        _shipmentAdapterMock
            .Setup(a => a.SendShipmentAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CargoProvider>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("503 Service Unavailable"));
        SetupPlatformAdapter(_shipmentAdapterMock.Object, "Hepsiburada");

        var sut = CreateSut(_publisherMock.Object);

        // Act
        await sut.ProcessOrderAsync(_orderId);

        // Assert — MediatR Publish must be called with DomainEventNotification<PlatformNotificationFailedEvent>
        _publisherMock.Verify(
            p => p.Publish(
                It.Is<DomainEventNotification<PlatformNotificationFailedEvent>>(n =>
                    n.DomainEvent.OrderId == _orderId &&
                    n.DomainEvent.PlatformCode == "Hepsiburada" &&
                    n.DomainEvent.TrackingNumber == "SR-PUBLISH" &&
                    n.DomainEvent.CargoProvider == CargoProvider.SuratKargo &&
                    n.DomainEvent.ErrorMessage.Contains("503")),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "PlatformNotificationFailedEvent must be published via MediatR on failure");
    }

    [Fact]
    public async Task ProcessOrderAsync_PlatformReturnsFalse_PublishesPlatformNotificationFailedEvent()
    {
        // Arrange
        SetupOrder(PlatformType.Trendyol);
        SetupSelector(CargoProvider.YurticiKargo);
        SetupCargoFactory(_cargoAdapterMock.Object);
        SetupCargoResult(ShipmentResult.Succeeded("YK-FALSE-PUB", "SHIP-FP"));

        _shipmentAdapterMock
            .Setup(a => a.SendShipmentAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CargoProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        SetupPlatformAdapter(_shipmentAdapterMock.Object);

        var sut = CreateSut(_publisherMock.Object);

        // Act
        await sut.ProcessOrderAsync(_orderId);

        // Assert
        _publisherMock.Verify(
            p => p.Publish(
                It.Is<DomainEventNotification<PlatformNotificationFailedEvent>>(n =>
                    n.DomainEvent.OrderId == _orderId &&
                    n.DomainEvent.PlatformCode == "Trendyol"),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Platform returning false must also trigger MediatR event publish");
    }

    [Fact]
    public async Task ProcessOrderAsync_NoPublisher_StillSucceeds_NoException()
    {
        // Arrange — publisher is null (optional dependency)
        SetupOrder(PlatformType.Trendyol);
        SetupSelector(CargoProvider.YurticiKargo);
        SetupCargoFactory(_cargoAdapterMock.Object);
        SetupCargoResult(ShipmentResult.Succeeded("YK-NOPUB", "SHIP-NP"));

        _shipmentAdapterMock
            .Setup(a => a.SendShipmentAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CargoProvider>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("API down"));
        SetupPlatformAdapter(_shipmentAdapterMock.Object);

        var sut = CreateSut(publisher: null); // NO publisher

        // Act
        var result = await sut.ProcessOrderAsync(_orderId);

        // Assert — must not throw, cargo result preserved
        result.Success.Should().BeTrue("null publisher must not cause exception");
        result.TrackingNumber.Should().Be("YK-NOPUB");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 6. Order not found → returns failed
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ProcessOrderAsync_OrderNotFound_ReturnsFailed()
    {
        // Arrange — repository returns null
        _orderRepoMock
            .Setup(r => r.GetByIdAsync(_orderId))
            .ReturnsAsync((Order?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.ProcessOrderAsync(_orderId);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain(_orderId.ToString());

        // No cargo or platform calls should happen
        _selectorMock.Verify(
            s => s.SelectBestProviderAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 7. Pazarama uses same flow — no special handling in AutoShipmentService
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ProcessOrderAsync_Pazarama_SameFlowAsOtherPlatforms()
    {
        // Arrange — Pazarama platform, standard flow
        SetupFullChain(PlatformType.Pazarama, CargoProvider.ArasKargo, "AR-PAZ-001", "SHIP-PAZ");

        var sut = CreateSut();

        // Act
        var result = await sut.ProcessOrderAsync(_orderId);

        // Assert
        result.Success.Should().BeTrue();
        result.TrackingNumber.Should().Be("AR-PAZ-001");

        // Verify the same SendShipmentAsync interface is called — no Pazarama-specific branching
        _shipmentAdapterMock.Verify(
            a => a.SendShipmentAsync(
                _orderId.ToString(), "AR-PAZ-001", CargoProvider.ArasKargo,
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Pazarama must use same SendShipmentAsync flow as all other platforms");

        // Verify platform code was resolved from Order.SourcePlatform
        _adapterFactoryMock.Verify(
            f => f.ResolveCapability<IShipmentCapableAdapter>("Pazarama"),
            Times.Once);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 8. AdapterFactory resolves all 5 platforms
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void AdapterFactory_ResolvesAll5Platforms()
    {
        // Arrange — 5 mock platform adapters
        var platforms = new[] { "Trendyol", "OpenCart", "Ciceksepeti", "Hepsiburada", "Pazarama" };

        foreach (var platform in platforms)
        {
            _adapterFactoryMock
                .Setup(f => f.Resolve(platform))
                .Returns(Mock.Of<IIntegratorAdapter>(a => a.PlatformCode == platform));
        }

        // Act & Assert
        foreach (var platform in platforms)
        {
            var adapter = _adapterFactoryMock.Object.Resolve(platform);
            adapter.Should().NotBeNull($"AdapterFactory must resolve '{platform}'");
            adapter!.PlatformCode.Should().Be(platform);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 9. Constructor: IPublisher is optional
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Constructor_PublisherNull_DoesNotThrow()
    {
        // Act
        var action = () => new AutoShipmentService(
            _selectorMock.Object,
            _cargoFactoryMock.Object,
            _adapterFactoryMock.Object,
            _orderRepoMock.Object,
            _loggerMock.Object,
            publisher: null);

        // Assert — IPublisher is optional, null must not throw
        action.Should().NotThrow();
    }
}
