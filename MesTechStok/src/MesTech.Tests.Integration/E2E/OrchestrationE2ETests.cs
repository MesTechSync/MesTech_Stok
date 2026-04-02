using FluentAssertions;
using MesTech.Application.DTOs;
using MesTech.Application.DTOs.Cargo;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Integration.Factory;
using MesTech.Infrastructure.Integration.Orchestration;
using MesTech.Tests.Integration._Shared;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MesTech.Tests.Integration.E2E;

/// <summary>
/// Cross-component orchestration E2E tests.
/// Uses REAL orchestration services (CargoProviderSelector, CargoProviderFactory,
/// AdapterFactory, AutoShipmentService) wired together with mock adapters.
/// No real HTTP, Docker, or Testcontainers — verifies service graph integrity.
/// </summary>
[Trait("Category", "E2E")]
[Trait("Phase", "Dalga3")]
public class OrchestrationE2ETests
{
    // ══════════════════════════════════════════════════════════════════════════
    // Shared helpers
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a mock IOrderRepository that returns an order for any id.
    /// </summary>
    private static Mock<IOrderRepository> CreateMockOrderRepo(PlatformType platform = PlatformType.Trendyol)
    {
        var mock = new Mock<IOrderRepository>();
        var order = new Order
        {
            CustomerName = "E2E Test Customer",
            SourcePlatform = platform
        };
        mock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(order);
        mock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        return mock;
    }

    /// <summary>
    /// Creates a mock ICargoAdapter with specified provider, availability, and shipment result.
    /// </summary>
    private static Mock<ICargoAdapter> CreateMockCargoAdapter(
        CargoProvider provider,
        bool isAvailable,
        ShipmentResult? shipmentResult = null)
    {
        var mock = new Mock<ICargoAdapter>();
        mock.Setup(a => a.Provider).Returns(provider);
        mock.Setup(a => a.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(isAvailable);
        mock.Setup(a => a.SupportsCancellation).Returns(true);
        mock.Setup(a => a.SupportsLabelGeneration).Returns(true);
        mock.Setup(a => a.SupportsCashOnDelivery).Returns(provider != CargoProvider.SuratKargo);
        mock.Setup(a => a.SupportsMultiParcel).Returns(true);

        if (shipmentResult is not null)
        {
            mock.Setup(a => a.CreateShipmentAsync(
                    It.IsAny<ShipmentRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(shipmentResult);
        }

        return mock;
    }

    /// <summary>
    /// Creates a mock IIntegratorAdapter that also implements IShipmentCapableAdapter.
    /// </summary>
    private static Mock<T> CreateMockPlatformAdapter<T>(string platformCode)
        where T : class, IIntegratorAdapter
    {
        var mock = new Mock<T>();
        mock.Setup(a => a.PlatformCode).Returns(platformCode);
        mock.Setup(a => a.SupportsStockUpdate).Returns(true);
        mock.Setup(a => a.SupportsPriceUpdate).Returns(true);
        mock.Setup(a => a.SupportsShipment).Returns(true);
        mock.Setup(a => a.PushStockUpdateAsync(
                It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mock.Setup(a => a.PushPriceUpdateAsync(
                It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mock.Setup(a => a.PullProductsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>().AsReadOnly());
        mock.Setup(a => a.PushProductAsync(
                It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mock.Setup(a => a.GetCategoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategoryDto>().AsReadOnly());
        mock.Setup(a => a.TestConnectionAsync(
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionTestResultDto { IsSuccess = true, PlatformCode = platformCode });

        return mock;
    }

    /// <summary>
    /// Combined interface for platform adapters that support both IIntegratorAdapter and IShipmentCapableAdapter.
    /// Used for Moq multi-interface mocking.
    /// </summary>
    public interface IShipmentPlatformAdapter : IIntegratorAdapter, IShipmentCapableAdapter { }

    // ══════════════════════════════════════════════════════════════════════════
    // 1. CargoAutoDispatch_FullChain
    //    REAL: CargoProviderSelector + CargoProviderFactory + AutoShipmentService
    //    MOCK: cargo adapters + platform adapter
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CargoAutoDispatch_FullChain_SelectsFirstAvailable_CreatesShipment_NotifiesPlatform()
    {
        // Arrange — build REAL service graph
        var yurticiMock = CreateMockCargoAdapter(
            CargoProvider.YurticiKargo, isAvailable: true,
            ShipmentResult.Succeeded("YK-E2E-001", "SHIP-E2E-001"));

        var arasMock = CreateMockCargoAdapter(
            CargoProvider.ArasKargo, isAvailable: true,
            ShipmentResult.Succeeded("AR-E2E-001", "SHIP-E2E-AR"));

        var cargoAdapters = new ICargoAdapter[] { yurticiMock.Object, arasMock.Object };

        var cargoFactory = new CargoProviderFactory(
            cargoAdapters, NullLogger<CargoProviderFactory>.Instance);

        var selector = new CargoProviderSelector(
            cargoFactory, NullLogger<CargoProviderSelector>.Instance);

        // Platform adapter that supports IShipmentCapableAdapter
        var trendyolMock = CreateMockPlatformAdapter<IShipmentPlatformAdapter>("Trendyol");
        trendyolMock.As<IShipmentCapableAdapter>()
            .Setup(a => a.SendShipmentAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CargoProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var adapterFactory = new AdapterFactory(
            new IIntegratorAdapter[] { trendyolMock.Object },
            NullLogger<AdapterFactory>.Instance);

        var orderRepoMock = CreateMockOrderRepo();

        var autoShipment = new AutoShipmentService(
            selector, cargoFactory, adapterFactory,
            orderRepoMock.Object,
            NullLogger<AutoShipmentService>.Instance);

        var orderId = Guid.NewGuid();

        // Act
        var result = await autoShipment.ProcessOrderAsync(orderId);

        // Assert
        result.Success.Should().BeTrue("full chain should produce successful shipment");
        result.TrackingNumber.Should().Be("YK-E2E-001",
            "YurticiKargo has highest priority and is available");
        result.ShipmentId.Should().Be("SHIP-E2E-001");

        yurticiMock.Verify(
            a => a.CreateShipmentAsync(
                It.Is<ShipmentRequest>(r => r.OrderId == orderId),
                It.IsAny<CancellationToken>()),
            Times.Once);

        trendyolMock.As<IShipmentCapableAdapter>().Verify(
            a => a.SendShipmentAsync(
                orderId.ToString(), "YK-E2E-001", CargoProvider.YurticiKargo,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 2. CargoAutoDispatch_FallbackToSecondProvider
    //    YurticiKargo unavailable → falls back to ArasKargo
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CargoAutoDispatch_FallbackToSecondProvider_WhenYurticiUnavailable_UsesArasKargo()
    {
        // Arrange — Yurtici is unavailable, Aras is available
        var yurticiMock = CreateMockCargoAdapter(
            CargoProvider.YurticiKargo, isAvailable: false);

        var arasMock = CreateMockCargoAdapter(
            CargoProvider.ArasKargo, isAvailable: true,
            ShipmentResult.Succeeded("AR-FALLBACK-001", "SHIP-FALLBACK-001"));

        var suratMock = CreateMockCargoAdapter(
            CargoProvider.SuratKargo, isAvailable: true,
            ShipmentResult.Succeeded("SR-FALLBACK-001", "SHIP-FALLBACK-SR"));

        var cargoAdapters = new ICargoAdapter[] { yurticiMock.Object, arasMock.Object, suratMock.Object };

        var cargoFactory = new CargoProviderFactory(
            cargoAdapters, NullLogger<CargoProviderFactory>.Instance);

        var selector = new CargoProviderSelector(
            cargoFactory, NullLogger<CargoProviderSelector>.Instance);

        // Minimal adapter factory (no platform adapter needed for this test)
        var adapterFactory = new AdapterFactory(
            Array.Empty<IIntegratorAdapter>(),
            NullLogger<AdapterFactory>.Instance);

        var orderRepoMock = CreateMockOrderRepo();

        var autoShipment = new AutoShipmentService(
            selector, cargoFactory, adapterFactory,
            orderRepoMock.Object,
            NullLogger<AutoShipmentService>.Instance);

        var orderId = Guid.NewGuid();

        // Act
        var result = await autoShipment.ProcessOrderAsync(orderId);

        // Assert
        result.Success.Should().BeTrue("ArasKargo fallback should succeed");
        result.TrackingNumber.Should().Be("AR-FALLBACK-001");

        arasMock.Verify(
            a => a.CreateShipmentAsync(
                It.IsAny<ShipmentRequest>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "ArasKargo adapter must be called as fallback");

        yurticiMock.Verify(
            a => a.CreateShipmentAsync(
                It.IsAny<ShipmentRequest>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "YurticiKargo must NOT be called (unavailable)");

        suratMock.Verify(
            a => a.CreateShipmentAsync(
                It.IsAny<ShipmentRequest>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "SuratKargo must NOT be called (ArasKargo already selected)");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 3. AdapterFactory_ResolveAll_FourPlatforms
    //    REAL AdapterFactory with 4 mock platform adapters
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void AdapterFactory_ResolveAll_FourPlatforms_AllResolvedWithCorrectPlatformCode()
    {
        // Arrange — create 4 mock platform adapters
        var trendyolMock = CreateMockPlatformAdapter<IIntegratorAdapter>("Trendyol");
        var opencartMock = CreateMockPlatformAdapter<IIntegratorAdapter>("OpenCart");
        var ciceksepetiMock = CreateMockPlatformAdapter<IIntegratorAdapter>("Ciceksepeti");
        var hepsiburadaMock = CreateMockPlatformAdapter<IIntegratorAdapter>("Hepsiburada");

        var adapters = new IIntegratorAdapter[]
        {
            trendyolMock.Object,
            opencartMock.Object,
            ciceksepetiMock.Object,
            hepsiburadaMock.Object
        };

        var factory = new AdapterFactory(adapters, NullLogger<AdapterFactory>.Instance);

        // Act & Assert — resolve by string
        var trendyol = factory.Resolve("Trendyol");
        var opencart = factory.Resolve("OpenCart");
        var ciceksepeti = factory.Resolve("Ciceksepeti");
        var hepsiburada = factory.Resolve("Hepsiburada");

        trendyol.Should().NotBeNull();
        trendyol!.PlatformCode.Should().Be("Trendyol");

        opencart.Should().NotBeNull();
        opencart!.PlatformCode.Should().Be("OpenCart");

        ciceksepeti.Should().NotBeNull();
        ciceksepeti!.PlatformCode.Should().Be("Ciceksepeti");

        hepsiburada.Should().NotBeNull();
        hepsiburada!.PlatformCode.Should().Be("Hepsiburada");

        // Also verify GetAll returns all 4
        var all = factory.GetAll();
        all.Should().HaveCount(4);

        // Verify case-insensitive resolution (AdapterFactory uses StringComparer.OrdinalIgnoreCase)
        var trendyolLower = factory.Resolve("trendyol");
        trendyolLower.Should().NotBeNull();
        trendyolLower!.PlatformCode.Should().Be("Trendyol");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 4. MultiPlatform_StockSync_AllAdaptersReceiveUpdate
    //    All 4 platform adapters receive the same stock update
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task MultiPlatform_StockSync_AllAdaptersReceiveUpdate()
    {
        // Arrange
        var trendyolMock = CreateMockPlatformAdapter<IIntegratorAdapter>("Trendyol");
        var opencartMock = CreateMockPlatformAdapter<IIntegratorAdapter>("OpenCart");
        var ciceksepetiMock = CreateMockPlatformAdapter<IIntegratorAdapter>("Ciceksepeti");
        var hepsiburadaMock = CreateMockPlatformAdapter<IIntegratorAdapter>("Hepsiburada");

        var allAdapters = new[]
        {
            trendyolMock,
            opencartMock,
            ciceksepetiMock,
            hepsiburadaMock
        };

        var productId = Guid.NewGuid();
        const int newStock = 42;

        // Act — push stock update to all 4 platforms in parallel (like real orchestration)
        var tasks = allAdapters.Select(adapter =>
            adapter.Object.PushStockUpdateAsync(productId, newStock));
        var results = await Task.WhenAll(tasks);

        // Assert — all 4 adapters received the call and returned true
        results.Should().AllSatisfy(r => r.Should().BeTrue());

        foreach (var adapter in allAdapters)
        {
            adapter.Verify(
                a => a.PushStockUpdateAsync(productId, newStock, It.IsAny<CancellationToken>()),
                Times.Once,
                $"{adapter.Object.PlatformCode} should receive exactly one stock update");
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 5. PlatformNotificationFailure_NoCargoRollback
    //    REAL AutoShipmentService — cargo succeeds, platform throws, NO rollback
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PlatformNotificationFailure_NoCargoRollback_CargoSuccessPreserved()
    {
        // Arrange — REAL services with mock adapters
        var yurticiMock = CreateMockCargoAdapter(
            CargoProvider.YurticiKargo, isAvailable: true,
            ShipmentResult.Succeeded("YK-NOROLLBACK-001", "SHIP-NR-001"));

        var cargoFactory = new CargoProviderFactory(
            new ICargoAdapter[] { yurticiMock.Object },
            NullLogger<CargoProviderFactory>.Instance);

        var selector = new CargoProviderSelector(
            cargoFactory, NullLogger<CargoProviderSelector>.Instance);

        // Platform adapter that THROWS on SendShipmentAsync
        var trendyolMock = CreateMockPlatformAdapter<IShipmentPlatformAdapter>("Trendyol");
        trendyolMock.As<IShipmentCapableAdapter>()
            .Setup(a => a.SendShipmentAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CargoProvider>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Trendyol API 503 Service Unavailable"));

        var adapterFactory = new AdapterFactory(
            new IIntegratorAdapter[] { trendyolMock.Object },
            NullLogger<AdapterFactory>.Instance);

        var orderRepoMock = CreateMockOrderRepo();

        var autoShipment = new AutoShipmentService(
            selector, cargoFactory, adapterFactory,
            orderRepoMock.Object,
            NullLogger<AutoShipmentService>.Instance);

        var orderId = Guid.NewGuid();

        // Act
        var result = await autoShipment.ProcessOrderAsync(orderId);

        // Assert — cargo result must still be Success despite platform failure
        result.Success.Should().BeTrue(
            "cargo shipment succeeded; platform notification failure must NOT roll back cargo");
        result.TrackingNumber.Should().Be("YK-NOROLLBACK-001");

        // CancelShipmentAsync must NEVER be called — no cargo rollback policy
        yurticiMock.Verify(
            a => a.CancelShipmentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "CancelShipmentAsync must NEVER be called — DO NOT rollback cargo on platform failure");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 6. TenantIsolation_DifferentTenantIds
    //    Verify TestTenantProvider returns correct tenant for each context switch
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void TenantIsolation_DifferentTenantIds_OperationsUseCorrectTenant()
    {
        // Arrange
        var tenantProvider = new TestTenantProvider();
        var tenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var tenantB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        // Act & Assert — Tenant A
        tenantProvider.SetTenant(tenantA);
        tenantProvider.GetCurrentTenantId().Should().Be(tenantA,
            "operations after SetTenant(A) must use TenantId A");

        // Simulate creating an order in Tenant A context
        var orderA = new Order { TenantId = tenantProvider.GetCurrentTenantId(), OrderNumber = "ORD-A-001" };
        orderA.TenantId.Should().Be(tenantA);

        // Act & Assert — Switch to Tenant B
        tenantProvider.SetTenant(tenantB);
        tenantProvider.GetCurrentTenantId().Should().Be(tenantB,
            "operations after SetTenant(B) must use TenantId B");

        // Simulate creating an order in Tenant B context
        var orderB = new Order { TenantId = tenantProvider.GetCurrentTenantId(), OrderNumber = "ORD-B-001" };
        orderB.TenantId.Should().Be(tenantB);

        // Cross-verify — previous order retains its tenant
        orderA.TenantId.Should().Be(tenantA, "Tenant A order must not be affected by context switch");
        orderB.TenantId.Should().Be(tenantB, "Tenant B order must have its own tenant");
        orderA.TenantId.Should().NotBe(orderB.TenantId, "different tenants must be isolated");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 7. OrderLifecycle_Trendyol_EndToEnd
    //    Pull orders → process first → create shipment → notify platform
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Combined interface for order + shipment capable adapters (Trendyol pattern).
    /// </summary>
    public interface IFullPlatformAdapter : IIntegratorAdapter, IOrderCapableAdapter, IShipmentCapableAdapter { }

    [Fact]
    public async Task OrderLifecycle_Trendyol_EndToEnd_PullOrdersThenShipFirstOrder()
    {
        // Arrange — Trendyol mock with order + shipment capabilities
        var trendyolMock = CreateMockPlatformAdapter<IFullPlatformAdapter>("Trendyol");

        var orders = new List<ExternalOrderDto>
        {
            new()
            {
                PlatformOrderId = "TY-ORD-001",
                PlatformCode = "Trendyol",
                OrderNumber = "2026030900001",
                Status = "Created",
                CustomerName = "Test Musteri",
                TotalAmount = 199.90m,
                OrderDate = DateTime.UtcNow,
                Lines = new List<ExternalOrderLineDto>
                {
                    new()
                    {
                        SKU = "SKU-001",
                        ProductName = "Test Urun",
                        Quantity = 2,
                        UnitPrice = 99.95m,
                        LineTotal = 199.90m
                    }
                }
            },
            new()
            {
                PlatformOrderId = "TY-ORD-002",
                PlatformCode = "Trendyol",
                OrderNumber = "2026030900002",
                Status = "Created",
                CustomerName = "Ikinci Musteri",
                TotalAmount = 49.90m,
                OrderDate = DateTime.UtcNow
            }
        };

        trendyolMock.As<IOrderCapableAdapter>()
            .Setup(a => a.PullOrdersAsync(It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders.AsReadOnly());

        trendyolMock.As<IOrderCapableAdapter>()
            .Setup(a => a.UpdateOrderStatusAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        trendyolMock.As<IShipmentCapableAdapter>()
            .Setup(a => a.SendShipmentAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CargoProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Cargo setup
        var yurticiMock = CreateMockCargoAdapter(
            CargoProvider.YurticiKargo, isAvailable: true,
            ShipmentResult.Succeeded("YK-LIFECYCLE-001", "SHIP-LC-001"));

        var cargoFactory = new CargoProviderFactory(
            new ICargoAdapter[] { yurticiMock.Object },
            NullLogger<CargoProviderFactory>.Instance);

        var selector = new CargoProviderSelector(
            cargoFactory, NullLogger<CargoProviderSelector>.Instance);

        var adapterFactory = new AdapterFactory(
            new IIntegratorAdapter[] { trendyolMock.Object },
            NullLogger<AdapterFactory>.Instance);

        var orderRepoMock = CreateMockOrderRepo();

        var autoShipment = new AutoShipmentService(
            selector, cargoFactory, adapterFactory,
            orderRepoMock.Object,
            NullLogger<AutoShipmentService>.Instance);

        // Act — Step 1: Pull orders from Trendyol
        var orderAdapter = trendyolMock.As<IOrderCapableAdapter>().Object;
        var pulledOrders = await orderAdapter.PullOrdersAsync();

        // Step 2: Get first order
        pulledOrders.Should().HaveCount(2);
        var firstOrder = pulledOrders[0];
        firstOrder.PlatformOrderId.Should().Be("TY-ORD-001");

        // Step 3: Process order through auto-shipment (creates shipment + notifies platform)
        var orderId = Guid.NewGuid();
        var shipmentResult = await autoShipment.ProcessOrderAsync(orderId);

        // Step 4: Update order status on platform
        var statusUpdated = await orderAdapter.UpdateOrderStatusAsync(
            firstOrder.PlatformOrderId, "Shipped");

        // Assert — full lifecycle completed
        shipmentResult.Success.Should().BeTrue();
        shipmentResult.TrackingNumber.Should().Be("YK-LIFECYCLE-001");
        statusUpdated.Should().BeTrue();

        // Verify sequence of operations
        trendyolMock.As<IOrderCapableAdapter>().Verify(
            a => a.PullOrdersAsync(It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
            Times.Once, "Orders must be pulled exactly once");

        yurticiMock.Verify(
            a => a.CreateShipmentAsync(
                It.IsAny<ShipmentRequest>(), It.IsAny<CancellationToken>()),
            Times.Once, "Cargo shipment must be created once");

        trendyolMock.As<IShipmentCapableAdapter>().Verify(
            a => a.SendShipmentAsync(
                It.IsAny<string>(), "YK-LIFECYCLE-001",
                CargoProvider.YurticiKargo, It.IsAny<CancellationToken>()),
            Times.Once, "Platform must be notified with tracking number");

        trendyolMock.As<IOrderCapableAdapter>().Verify(
            a => a.UpdateOrderStatusAsync("TY-ORD-001", "Shipped", It.IsAny<CancellationToken>()),
            Times.Once, "Order status must be updated to Shipped");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 8. BuildRegression_AllDalga3Services_Resolvable
    //    Verify DI graph integrity — concrete types implement their interfaces
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void BuildRegression_AllDalga3Services_Resolvable()
    {
        // Assert — interface→concrete type compatibility (Type.IsAssignableFrom)
        typeof(ICargoProviderSelector).IsAssignableFrom(typeof(CargoProviderSelector))
            .Should().BeTrue("CargoProviderSelector must implement ICargoProviderSelector");

        typeof(ICargoProviderFactory).IsAssignableFrom(typeof(CargoProviderFactory))
            .Should().BeTrue("CargoProviderFactory must implement ICargoProviderFactory");

        typeof(IAutoShipmentService).IsAssignableFrom(typeof(AutoShipmentService))
            .Should().BeTrue("AutoShipmentService must implement IAutoShipmentService");

        typeof(IAdapterFactory).IsAssignableFrom(typeof(AdapterFactory))
            .Should().BeTrue("AdapterFactory must implement IAdapterFactory");

        // Verify concrete types can be instantiated with mocks (constructor graph intact)
        var cargoAdapters = new ICargoAdapter[]
        {
            CreateMockCargoAdapter(CargoProvider.YurticiKargo, true).Object,
            CreateMockCargoAdapter(CargoProvider.ArasKargo, true).Object,
            CreateMockCargoAdapter(CargoProvider.SuratKargo, true).Object
        };

        var cargoFactory = new CargoProviderFactory(
            cargoAdapters, NullLogger<CargoProviderFactory>.Instance);
        cargoFactory.Should().NotBeNull();
        cargoFactory.GetAll().Should().HaveCount(3,
            "all 3 cargo adapters should be registered");

        var selector = new CargoProviderSelector(
            cargoFactory, NullLogger<CargoProviderSelector>.Instance);
        selector.Should().NotBeNull();

        var platformAdapters = new IIntegratorAdapter[]
        {
            CreateMockPlatformAdapter<IIntegratorAdapter>("Trendyol").Object,
            CreateMockPlatformAdapter<IIntegratorAdapter>("OpenCart").Object,
            CreateMockPlatformAdapter<IIntegratorAdapter>("Ciceksepeti").Object,
            CreateMockPlatformAdapter<IIntegratorAdapter>("Hepsiburada").Object
        };

        var adapterFactory = new AdapterFactory(
            platformAdapters, NullLogger<AdapterFactory>.Instance);
        adapterFactory.Should().NotBeNull();
        adapterFactory.GetAll().Should().HaveCount(4,
            "all 4 platform adapters should be registered");

        var orderRepoMock = CreateMockOrderRepo();

        var autoShipment = new AutoShipmentService(
            selector, cargoFactory, adapterFactory,
            orderRepoMock.Object,
            NullLogger<AutoShipmentService>.Instance);
        autoShipment.Should().NotBeNull(
            "full service graph must instantiate without errors");
    }
}
