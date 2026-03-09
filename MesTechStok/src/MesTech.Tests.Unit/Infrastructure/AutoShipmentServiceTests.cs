using FluentAssertions;
using MesTech.Application.DTOs.Cargo;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Integration.Orchestration;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Infrastructure;

/// <summary>
/// AutoShipmentService orkestrasyon testleri.
/// Tam zincir: selector → cargoFactory → cargoAdapter → adapterFactory → platformAdapter.
/// Cargo rollback olmadigi (DO NOT rollback) ve platform hata yonetimi dogrulanir.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "AutoShipment")]
[Trait("Phase", "Dalga3")]
public class AutoShipmentServiceTests
{
    private readonly Mock<ICargoProviderSelector> _selectorMock;
    private readonly Mock<ICargoProviderFactory> _cargoFactoryMock;
    private readonly Mock<IAdapterFactory> _adapterFactoryMock;
    private readonly Mock<IOrderRepository> _orderRepoMock;
    private readonly Mock<ILogger<AutoShipmentService>> _loggerMock;
    private readonly Mock<ICargoAdapter> _cargoAdapterMock;
    private readonly Mock<IShipmentCapableAdapter> _shipmentAdapterMock;
    private readonly AutoShipmentService _sut;

    private readonly Guid _orderId = Guid.NewGuid();

    public AutoShipmentServiceTests()
    {
        _selectorMock = new Mock<ICargoProviderSelector>();
        _cargoFactoryMock = new Mock<ICargoProviderFactory>();
        _adapterFactoryMock = new Mock<IAdapterFactory>();
        _orderRepoMock = new Mock<IOrderRepository>();
        _loggerMock = new Mock<ILogger<AutoShipmentService>>();
        _cargoAdapterMock = new Mock<ICargoAdapter>();
        _shipmentAdapterMock = new Mock<IShipmentCapableAdapter>();

        // Default: return an order for any GetByIdAsync call
        _orderRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new Order
            {
                CustomerName = "Test Customer",
                SourcePlatform = PlatformType.Trendyol
            });

        _sut = new AutoShipmentService(
            _selectorMock.Object,
            _cargoFactoryMock.Object,
            _adapterFactoryMock.Object,
            _orderRepoMock.Object,
            _loggerMock.Object);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void SetupSelector(CargoProvider provider = CargoProvider.YurticiKargo) =>
        _selectorMock
            .Setup(s => s.SelectBestProviderAsync(
                It.IsAny<MesTech.Domain.Entities.Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

    private void SetupCargoFactory(ICargoAdapter? adapter) =>
        _cargoFactoryMock
            .Setup(f => f.Resolve(It.IsAny<CargoProvider>()))
            .Returns(adapter);

    private void SetupCargoResult(ShipmentResult result) =>
        _cargoAdapterMock
            .Setup(a => a.CreateShipmentAsync(
                It.IsAny<ShipmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

    private void SetupPlatformAdapter(IShipmentCapableAdapter? adapter) =>
        _adapterFactoryMock
            .Setup(f => f.ResolveCapability<IShipmentCapableAdapter>(It.IsAny<string>()))
            .Returns(adapter);

    // ══════════════════════════════════════════════════════════════════════════
    // 1. Full success chain
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ProcessOrderAsync_FullSuccessChain_ReturnsSuccessWithTrackingNumber()
    {
        // Arrange
        var expectedResult = ShipmentResult.Succeeded("YK123456789", "SHIP-001");
        SetupSelector(CargoProvider.YurticiKargo);
        SetupCargoFactory(_cargoAdapterMock.Object);
        SetupCargoResult(expectedResult);

        _shipmentAdapterMock
            .Setup(a => a.SendShipmentAsync(
                _orderId.ToString(), "YK123456789", CargoProvider.YurticiKargo,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        SetupPlatformAdapter(_shipmentAdapterMock.Object);

        // Act
        var result = await _sut.ProcessOrderAsync(_orderId);

        // Assert
        result.Success.Should().BeTrue();
        result.TrackingNumber.Should().Be("YK123456789");
        result.ShipmentId.Should().Be("SHIP-001");

        _cargoAdapterMock.Verify(
            a => a.CreateShipmentAsync(
                It.Is<ShipmentRequest>(r => r.OrderId == _orderId),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _shipmentAdapterMock.Verify(
            a => a.SendShipmentAsync(
                _orderId.ToString(), "YK123456789", CargoProvider.YurticiKargo,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 2. Cargo adapter not found → return failed, no platform call
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ProcessOrderAsync_CargoAdapterNotFound_ReturnsFailed_NoPlatformCall()
    {
        // Arrange
        SetupSelector(CargoProvider.ArasKargo);
        SetupCargoFactory(null); // no adapter registered for ArasKargo

        // Act
        var result = await _sut.ProcessOrderAsync(_orderId);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("ArasKargo");

        _adapterFactoryMock.Verify(
            f => f.ResolveCapability<IShipmentCapableAdapter>(It.IsAny<string>()),
            Times.Never,
            "Platform notification must not be attempted when cargo adapter is missing");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 3. Cargo shipment creation fails → return failed, no platform call
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ProcessOrderAsync_CargoCreationFails_ReturnsFailed_NoPlatformCall()
    {
        // Arrange
        SetupSelector(CargoProvider.SuratKargo);
        SetupCargoFactory(_cargoAdapterMock.Object);
        SetupCargoResult(ShipmentResult.Failed("SOAP timeout"));

        // Act
        var result = await _sut.ProcessOrderAsync(_orderId);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("SOAP timeout");

        _adapterFactoryMock.Verify(
            f => f.ResolveCapability<IShipmentCapableAdapter>(It.IsAny<string>()),
            Times.Never,
            "Platform must not be notified when cargo creation failed");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 4. Platform notification throws exception → cargo SUCCESS preserved (no rollback)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ProcessOrderAsync_PlatformNotificationThrows_CargoSuccessPreserved_NoRollback()
    {
        // Arrange
        SetupSelector(CargoProvider.ArasKargo);
        SetupCargoFactory(_cargoAdapterMock.Object);
        SetupCargoResult(ShipmentResult.Succeeded("AR987654321", "SHIP-002"));

        _shipmentAdapterMock
            .Setup(a => a.SendShipmentAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CargoProvider>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Platform API unreachable"));
        SetupPlatformAdapter(_shipmentAdapterMock.Object);

        // Act
        var result = await _sut.ProcessOrderAsync(_orderId);

        // Assert — exception must be absorbed; cargo result is preserved
        result.Success.Should().BeTrue(
            "Cargo succeeded; platform failure must not roll back the cargo shipment");
        result.TrackingNumber.Should().Be("AR987654321");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 5. Platform notification returns false → cargo SUCCESS preserved, no rollback
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ProcessOrderAsync_PlatformNotificationReturnsFalse_CargoSuccessPreserved()
    {
        // Arrange
        SetupSelector(CargoProvider.SuratKargo);
        SetupCargoFactory(_cargoAdapterMock.Object);
        SetupCargoResult(ShipmentResult.Succeeded("SR111222333", "SHIP-003"));

        _shipmentAdapterMock
            .Setup(a => a.SendShipmentAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CargoProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        SetupPlatformAdapter(_shipmentAdapterMock.Object);

        // Act
        var result = await _sut.ProcessOrderAsync(_orderId);

        // Assert — false triggers PlatformNotificationFailedEvent, not cargo rollback
        result.Success.Should().BeTrue(
            "Cargo was created successfully; platform 'false' only queues a retry event");
        result.TrackingNumber.Should().Be("SR111222333");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 6. No IShipmentCapableAdapter → graceful skip, returns cargo success
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ProcessOrderAsync_NoShipmentCapableAdapter_SkipsNotification_ReturnsCargoSuccess()
    {
        // Arrange
        SetupSelector(CargoProvider.YurticiKargo);
        SetupCargoFactory(_cargoAdapterMock.Object);
        SetupCargoResult(ShipmentResult.Succeeded("YK000111222", "SHIP-004"));
        SetupPlatformAdapter(null); // platform does not support IShipmentCapableAdapter

        // Act
        var result = await _sut.ProcessOrderAsync(_orderId);

        // Assert
        result.Success.Should().BeTrue();

        _shipmentAdapterMock.Verify(
            a => a.SendShipmentAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CargoProvider>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "SendShipmentAsync must not be called when capability is absent");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 7. OperationCanceledException propagates (not swallowed)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ProcessOrderAsync_WhenCancelled_PropagatesCancellation()
    {
        // Arrange
        _selectorMock
            .Setup(s => s.SelectBestProviderAsync(
                It.IsAny<MesTech.Domain.Entities.Order>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var act = () => _sut.ProcessOrderAsync(_orderId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 8. Constructor null guards
    // ══════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("selector")]
    [InlineData("cargoFactory")]
    [InlineData("adapterFactory")]
    [InlineData("orderRepository")]
    [InlineData("logger")]
    public void Constructor_NullDependency_ThrowsArgumentNullException(string nullParam)
    {
        Action action = nullParam switch
        {
            "selector"        => () => new AutoShipmentService(null!, _cargoFactoryMock.Object, _adapterFactoryMock.Object, _orderRepoMock.Object, _loggerMock.Object),
            "cargoFactory"    => () => new AutoShipmentService(_selectorMock.Object, null!, _adapterFactoryMock.Object, _orderRepoMock.Object, _loggerMock.Object),
            "adapterFactory"  => () => new AutoShipmentService(_selectorMock.Object, _cargoFactoryMock.Object, null!, _orderRepoMock.Object, _loggerMock.Object),
            "orderRepository" => () => new AutoShipmentService(_selectorMock.Object, _cargoFactoryMock.Object, _adapterFactoryMock.Object, null!, _loggerMock.Object),
            "logger"          => () => new AutoShipmentService(_selectorMock.Object, _cargoFactoryMock.Object, _adapterFactoryMock.Object, _orderRepoMock.Object, null!),
            _                 => throw new ArgumentOutOfRangeException(nameof(nullParam))
        };

        action.Should().Throw<ArgumentNullException>().WithParameterName(nullParam);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 9. OrderId is threaded through to ShipmentRequest
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ProcessOrderAsync_OrderIdIsPassedToShipmentRequest()
    {
        // Arrange
        var specificOrderId = Guid.Parse("aaaabbbb-cccc-dddd-eeee-ffffffffffff");
        SetupSelector(CargoProvider.YurticiKargo);
        SetupCargoFactory(_cargoAdapterMock.Object);
        _cargoAdapterMock
            .Setup(a => a.CreateShipmentAsync(It.IsAny<ShipmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ShipmentResult.Succeeded("YK-VERIFY", "SHIP-V"));
        SetupPlatformAdapter(null);

        // Act
        await _sut.ProcessOrderAsync(specificOrderId);

        // Assert
        _cargoAdapterMock.Verify(
            a => a.CreateShipmentAsync(
                It.Is<ShipmentRequest>(r => r.OrderId == specificOrderId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 10. Selector is called exactly once per invocation
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ProcessOrderAsync_SelectorCalledExactlyOnce()
    {
        // Arrange
        SetupSelector(CargoProvider.YurticiKargo);
        SetupCargoFactory(_cargoAdapterMock.Object);
        SetupCargoResult(ShipmentResult.Succeeded("YK-SEL", "SHIP-S"));
        SetupPlatformAdapter(null);

        // Act
        await _sut.ProcessOrderAsync(_orderId);

        // Assert
        _selectorMock.Verify(
            s => s.SelectBestProviderAsync(It.IsAny<MesTech.Domain.Entities.Order>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
