using System.Net.Http;
using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Integration.Adapters;
using Microsoft.Extensions.Logging;

namespace MesTech.Tests.Integration.Adapters;

/// <summary>
/// Trendyol GERCEK sandbox API testleri.
/// Ortam degiskenleri ayarlanmadiysa testler SKIP edilir.
/// Calistirmak icin:
///   TRENDYOL_SANDBOX_ENABLED=true
///   TRENDYOL_API_KEY=...
///   TRENDYOL_API_SECRET=...
///   TRENDYOL_SUPPLIER_ID=... (varsayilan: 1076956)
/// </summary>
[Trait("Category", "Sandbox")]
[Trait("Platform", "Trendyol")]
public class TrendyolAdapterSandboxTests : IDisposable
{
    private const string SandboxBaseUrl = "https://stage-apigw.trendyol.com/integration";
    private const string DefaultSupplierId = "1076956";

    private readonly HttpClient _httpClient;
    private readonly ILogger<TrendyolAdapter> _logger;
    private readonly TrendyolAdapter _adapter;
    private readonly Dictionary<string, string> _credentials;
    private readonly bool _isSandboxEnabled;

    public TrendyolAdapterSandboxTests()
    {
        var sandboxEnabled = Environment.GetEnvironmentVariable("TRENDYOL_SANDBOX_ENABLED");
        var apiKey = Environment.GetEnvironmentVariable("TRENDYOL_API_KEY");
        var apiSecret = Environment.GetEnvironmentVariable("TRENDYOL_API_SECRET");
        var supplierId = Environment.GetEnvironmentVariable("TRENDYOL_SUPPLIER_ID") ?? DefaultSupplierId;

        _isSandboxEnabled = string.Equals(sandboxEnabled, "true", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(apiKey)
            && !string.IsNullOrWhiteSpace(apiSecret);

        _httpClient = new HttpClient { BaseAddress = new Uri(SandboxBaseUrl) };
        _logger = new LoggerFactory().CreateLogger<TrendyolAdapter>();
        _adapter = new TrendyolAdapter(_httpClient, _logger);

        _credentials = new Dictionary<string, string>
        {
            ["ApiKey"] = apiKey ?? string.Empty,
            ["ApiSecret"] = apiSecret ?? string.Empty,
            ["SupplierId"] = supplierId
        };
    }

    private void SkipIfNotEnabled()
    {
        Skip.If(!_isSandboxEnabled,
            "Trendyol sandbox credentials not configured. " +
            "Set TRENDYOL_SANDBOX_ENABLED=true, TRENDYOL_API_KEY, TRENDYOL_API_SECRET to run.");
    }

    /// <summary>
    /// Adapter'i yapilandirip sandbox'a baglantiyi dogrular.
    /// Diger testlerden once cagrilir.
    /// </summary>
    private async Task<TrendyolAdapter> ConfigureAdapterAsync()
    {
        var result = await _adapter.TestConnectionAsync(_credentials);
        result.IsSuccess.Should().BeTrue(
            "Sandbox baglantisi basarili olmali — hata: {0}", result.ErrorMessage ?? "yok");
        return _adapter;
    }

    // ══════════════════════════════════════
    // 1. Connection Test
    // ══════════════════════════════════════

    [SkippableFact]
    public async Task TestConnectionAsync_RealSandbox_ReturnsSuccess()
    {
        // Arrange
        SkipIfNotEnabled();

        // Act
        var result = await _adapter.TestConnectionAsync(_credentials);

        // Assert
        result.IsSuccess.Should().BeTrue(
            "Sandbox baglanti testi basarili olmali — hata: {0}", result.ErrorMessage ?? "yok");
        result.HttpStatusCode.Should().Be(200);
        result.ResponseTime.Should().BeGreaterThan(TimeSpan.Zero);
        result.StoreName.Should().NotBeNullOrWhiteSpace();
    }

    // ══════════════════════════════════════
    // 2. Pull Products
    // ══════════════════════════════════════

    [SkippableFact]
    public async Task PullProductsAsync_RealSandbox_ReturnsProducts()
    {
        // Arrange
        SkipIfNotEnabled();
        await ConfigureAdapterAsync();

        // Act
        var products = await _adapter.PullProductsAsync();

        // Assert
        products.Should().NotBeNull("PullProductsAsync null donmemeli");
        // Sandbox'ta urun olmayabilir ama liste bos degil hatasi olmasin
        // En azindan cagri basarili olmali (exception atmasin)
        products.Count.Should().BeGreaterThanOrEqualTo(0,
            "Sandbox urun listesi null degil, bos bile olsa >= 0 olmali");
    }

    // ══════════════════════════════════════
    // 3. Pull Orders
    // ══════════════════════════════════════

    [SkippableFact]
    public async Task PullOrdersAsync_RealSandbox_ReturnsOrders()
    {
        // Arrange
        SkipIfNotEnabled();
        var adapter = await ConfigureAdapterAsync();
        var orderAdapter = adapter as IOrderCapableAdapter;
        orderAdapter.Should().NotBeNull("TrendyolAdapter IOrderCapableAdapter implement etmeli");

        // Act — son 30 gunluk siparisler
        var orders = await orderAdapter!.PullOrdersAsync(since: DateTime.UtcNow.AddDays(-30));

        // Assert
        orders.Should().NotBeNull("PullOrdersAsync null donmemeli");
        orders.Count.Should().BeGreaterThanOrEqualTo(0,
            "Sandbox siparis listesi null degil, bos bile olsa >= 0 olmali");
    }

    // ══════════════════════════════════════
    // 4. Get Categories
    // ══════════════════════════════════════

    [SkippableFact]
    public async Task GetCategoriesAsync_RealSandbox_ReturnsCategories()
    {
        // Arrange
        SkipIfNotEnabled();
        await ConfigureAdapterAsync();

        // Act
        var categories = await _adapter.GetCategoriesAsync();

        // Assert
        categories.Should().NotBeNull("GetCategoriesAsync null donmemeli");
        categories.Should().NotBeEmpty("Trendyol kategorileri bos olmamali — API her zaman kategori doner");
        categories.First().Name.Should().NotBeNullOrWhiteSpace(
            "Ilk kategorinin ismi bos olmamali");
    }

    // ══════════════════════════════════════
    // 5. Push Stock Update
    // ══════════════════════════════════════

    [SkippableFact]
    public async Task PushStockUpdateAsync_RealSandbox_NoException()
    {
        // Arrange
        SkipIfNotEnabled();
        await ConfigureAdapterAsync();

        // Act — sandbox'ta gecersiz product ID ile bile exception atmasin
        // Sandbox baglantisi dogrulama amacli — false donebilir ama exception atmasin
        var act = async () => await _adapter.PushStockUpdateAsync(Guid.NewGuid(), 10);

        // Assert — exception atmasin (false donmesi kabul edilir)
        await act.Should().NotThrowAsync(
            "PushStockUpdateAsync exception firlatmamali — API hatasi false olarak donmeli");
    }

    // ══════════════════════════════════════
    // 6. Push Price Update
    // ══════════════════════════════════════

    [SkippableFact]
    public async Task PushPriceUpdateAsync_RealSandbox_NoException()
    {
        // Arrange
        SkipIfNotEnabled();
        await ConfigureAdapterAsync();

        // Act — sandbox'ta gecersiz product ID ile bile exception atmasin
        var act = async () => await _adapter.PushPriceUpdateAsync(Guid.NewGuid(), 99.99m);

        // Assert — exception atmasin (false donmesi kabul edilir)
        await act.Should().NotThrowAsync(
            "PushPriceUpdateAsync exception firlatmamali — API hatasi false olarak donmeli");
    }

    // ══════════════════════════════════════
    // 7. Send Shipment (UpdateOrderStatus)
    // ══════════════════════════════════════

    [SkippableFact]
    public async Task SendShipmentAsync_RealSandbox_NoException()
    {
        // Arrange
        SkipIfNotEnabled();
        var adapter = await ConfigureAdapterAsync();
        var orderAdapter = adapter as IOrderCapableAdapter;
        orderAdapter.Should().NotBeNull("TrendyolAdapter IOrderCapableAdapter implement etmeli");

        // Act — Trendyol kargo bildirimi: UpdateOrderStatusAsync(packageId, "Shipped")
        // Sandbox'ta gecersiz packageId ile exception atmasin — false donmesi kabul edilir
        var act = async () => await orderAdapter!.UpdateOrderStatusAsync(
            "sandbox-pkg-test-001", "Shipped");

        // Assert — exception atmasin
        await act.Should().NotThrowAsync(
            "UpdateOrderStatusAsync (kargo bildirimi) exception firlatmamali — API hatasi false olarak donmeli");
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
