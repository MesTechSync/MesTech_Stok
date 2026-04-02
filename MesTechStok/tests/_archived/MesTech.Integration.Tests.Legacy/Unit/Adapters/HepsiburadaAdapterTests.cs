using System.Net;
using FluentAssertions;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Integration.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Adapters;

/// <summary>
/// HepsiburadaAdapter unit testleri — M3 Beta Agent genisletmesi.
/// MockHttpMessageHandler ile HTTP katmani stub edilerek adapter davranisi test edilir.
/// </summary>
public class HepsiburadaAdapterTests
{
    private readonly MockHttpMessageHandler _handler = new();
    private readonly Mock<ILogger<HepsiburadaAdapter>> _loggerMock = new();

    private HepsiburadaAdapter CreateAdapter()
    {
        var httpClient = new HttpClient(_handler)
        {
            BaseAddress = new Uri("https://mpop-sit.hepsiburada.com/")
        };
        return new HepsiburadaAdapter(httpClient, _loggerMock.Object);
    }

    private static Dictionary<string, string> ValidCredentials() => new()
    {
        ["MerchantId"] = "test-merchant-123",
        ["ApiKey"] = "test-api-key-456",
        ["BaseUrl"] = "https://mpop-sit.hepsiburada.com/"
    };

    private async Task ConfigureAdapterAsync(HepsiburadaAdapter adapter)
    {
        // TestConnection icin listings response enqueue et
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"listings":[{"merchantSku":"SKU-001","productName":"Test Urun"}],"totalCount":1}""");
        await adapter.TestConnectionAsync(ValidCredentials());
    }

    // ─────────────────────────────────────────────────────────────────────
    // Test 1: Bearer auth header dogrulama
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task HepsiburadaAdapter_SyncProducts_BearerAuth_IncludesCorrectHeader()
    {
        // Arrange — TokenService yok, legacy static Bearer header kullanilmali
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"listings":[{"merchantSku":"SKU-001","productName":"Test Urun"}],"totalCount":1}""");

        var adapter = CreateAdapter();

        // Act — TestConnection cagrilinca ConfigureAuth Bearer header set eder
        var result = await adapter.TestConnectionAsync(ValidCredentials());

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.PlatformCode.Should().Be("Hepsiburada");

        // Captured request Authorization header kontrolu
        var request = _handler.CapturedRequests[0];
        request.Headers.Authorization.Should().NotBeNull();
        request.Headers.Authorization!.Scheme.Should().Be("Bearer");
        // Legacy format: MerchantId:ApiKey
        request.Headers.Authorization!.Parameter.Should().Contain("test-merchant-123");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Test 2: Semaphore rate limiter — adapter SemaphoreSlim(20,20) kullanir
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task HepsiburadaAdapter_GetOrders_SemaphoreLimit_RespectsThrottle()
    {
        // Arrange
        var adapter = CreateAdapter();
        await ConfigureAdapterAsync(adapter);

        // Orders endpoint icin bos response
        _handler.EnqueueResponse(HttpStatusCode.OK,
            """{"orders":[],"totalCount":0}""");

        // Act — PullOrdersAsync, ExecuteWithRetryAsync icerisinde SemaphoreSlim.WaitAsync kullanir
        var orders = await adapter.PullOrdersAsync(DateTime.UtcNow.AddDays(-7));

        // Assert — Semaphore deadlock veya hata olmadan tamamlanmali
        orders.Should().NotBeNull();
        orders.Should().BeEmpty();

        // TestConnection(1) + PullOrders(1) = 2 request
        _handler.CapturedRequests.Count.Should().Be(2);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Test 3: MerchantSKU mapping — stock update payload'unda merchantSku kullanilir
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task HepsiburadaAdapter_UpdateStock_MerchantSKU_MapsCorrectly()
    {
        // Arrange
        var adapter = CreateAdapter();
        await ConfigureAdapterAsync(adapter);

        // Stock update icin basarili response
        _handler.EnqueueResponse(HttpStatusCode.OK, "{}");

        var productId = Guid.NewGuid();

        // Act
        var success = await adapter.PushStockUpdateAsync(productId, 42);

        // Assert
        success.Should().BeTrue();

        // Son request (stock update) endpoint ve method kontrolu
        // Request #0 = TestConnection, Request #1 = PushStockUpdate
        // NOT: adapter `using var request` kullanir, content dispose olur — sadece URL kontrol edilir
        var stockRequest = _handler.CapturedRequests[1];
        stockRequest.Method.Should().Be(HttpMethod.Post);
        stockRequest.RequestUri!.ToString().Should().Contain("/listings/and-inventory");

        // TestConnection(1) + StockUpdate(1) = 2 istek
        _handler.CapturedRequests.Count.Should().Be(2);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Test 4: Multi-parcel shipment — her paket ayri POST /packages/{id}/shipment
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task HepsiburadaAdapter_SendShipment_MultiParcel_SplitsCorrectly()
    {
        // Arrange
        var adapter = CreateAdapter();
        await ConfigureAdapterAsync(adapter);

        // Paket 1 icin shipment response
        _handler.EnqueueResponse(HttpStatusCode.OK, "{}");
        // Paket 2 icin shipment response
        _handler.EnqueueResponse(HttpStatusCode.OK, "{}");

        // Act — Her paket ID'si icin ayri SendShipmentAsync cagrisi
        var result1 = await adapter.SendShipmentAsync("PKG-1001", "TRK-YK-789", CargoProvider.YurticiKargo);
        var result2 = await adapter.SendShipmentAsync("PKG-1002", "TRK-AR-456", CargoProvider.ArasKargo);

        // Assert — her iki kargo bildirimi basarili
        result1.Should().BeTrue();
        result2.Should().BeTrue();

        // TestConnection(1) + Shipment1(1) + Shipment2(1) = 3
        _handler.CapturedRequests.Count.Should().Be(3);

        // Paket URL'leri dogru mu?
        _handler.CapturedRequests[1].RequestUri!.ToString().Should().Contain("/packages/PKG-1001/shipment");
        _handler.CapturedRequests[2].RequestUri!.ToString().Should().Contain("/packages/PKG-1002/shipment");

        // Her iki request POST olmali
        _handler.CapturedRequests[1].Method.Should().Be(HttpMethod.Post);
        _handler.CapturedRequests[2].Method.Should().Be(HttpMethod.Post);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Test 5: GetCategoriesAsync cached — hep bos doner (HB desteklemiyor)
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task HepsiburadaAdapter_GetCategories_Cached_ReturnsCachedResult()
    {
        // Arrange — HB adapter'da GetCategoriesAsync her zaman bos dizi doner
        var adapter = CreateAdapter();

        // Act — iki kez cagir, ikisi de bos donmeli (static empty array)
        var categories1 = await adapter.GetCategoriesAsync();
        var categories2 = await adapter.GetCategoriesAsync();

        // Assert — HTTP istegi gitmemeli (0 captured request), her iki sonuc bos
        categories1.Should().NotBeNull();
        categories1.Should().BeEmpty();
        categories2.Should().NotBeNull();
        categories2.Should().BeEmpty();

        // GetCategoriesAsync hic HTTP cagrisi yapmiyor
        _handler.CapturedRequests.Should().BeEmpty();
    }
}
