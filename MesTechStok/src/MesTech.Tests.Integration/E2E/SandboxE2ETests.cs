namespace MesTech.Tests.Integration.E2E;

/// <summary>
/// 5-02: Gerçek sandbox E2E — Trendyol sipariş → fatura → kargo flow.
/// 5-03: Cross-platform stok tutarlılığı — 5 platformda aynı ürün.
///
/// Aktifleştirmek için:
///   • TRENDYOL_API_KEY + TRENDYOL_API_SECRET env var set et
///   • SOVOS_USERNAME + SOVOS_PASSWORD env var set et
///   • YURTICI_ACCOUNT env var set et
/// </summary>
[Trait("Category", "E2E")]
[Trait("Requires", "RealCredentials")]
[Trait("Phase", "Dalga6")]
public class SandboxE2ETests
{
    private const string SkipReason =
        "Real sandbox credentials required. " +
        "Set TRENDYOL_API_KEY, TRENDYOL_API_SECRET, SOVOS_USERNAME, SOVOS_PASSWORD " +
        "env vars before enabling. Activate in H30 after KOMUTAN provides API keys.";

    /// <summary>
    /// 5-02: Trendyol sandbox → sipariş çek → e-Arşiv fatura kes → kargo etiketi oluştur.
    /// Tam E2E flow — gerçek API'ler, gerçek sandbox verisi.
    /// </summary>
    [Fact(Skip = SkipReason)]
    public async Task TrendyolOrder_ToEArsivInvoice_ToCargoLabel_FullFlow()
    {
        // AŞAMA 1: Trendyol sandbox'tan sipariş çek
        // var apiKey    = Environment.GetEnvironmentVariable("TRENDYOL_API_KEY")!;
        // var apiSecret = Environment.GetEnvironmentVariable("TRENDYOL_API_SECRET")!;
        // var adapter   = new TrendyolAdapter(apiKey, apiSecret, sellerId: "1076956", isTest: true);
        // var tenantId  = Guid.NewGuid();
        // var orders    = await adapter.GetOrdersAsync(tenantId, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);
        // orders.Should().NotBeEmpty("sandbox'ta en az 1 test siparişi olmalı");

        // AŞAMA 2: İlk siparişe Sovos e-Arşiv fatura kes
        // var sovosUser = Environment.GetEnvironmentVariable("SOVOS_USERNAME")!;
        // var sovosPass = Environment.GetEnvironmentVariable("SOVOS_PASSWORD")!;
        // var invoiceProvider = new SovosInvoiceProvider(sovosUser, sovosPass, isTest: true);
        // var invoiceResult = await invoiceProvider.CreateInvoiceAsync(MapOrderToInvoice(orders[0]));
        // invoiceResult.IsSuccess.Should().BeTrue("Sovos sandbox fatura kesilmeli");
        // invoiceResult.InvoiceNumber.Should().NotBeNullOrEmpty();

        // AŞAMA 3: Kargo etiketi oluştur (Yurtiçi sandbox)
        // var yurtici = new YurticiCargoProvider(Environment.GetEnvironmentVariable("YURTICI_ACCOUNT")!);
        // var shipment = await yurtici.CreateShipmentAsync(orders[0], waybillNo: invoiceResult.InvoiceNumber);
        // shipment.TrackingNumber.Should().NotBeNullOrEmpty();
        // shipment.CargoBarcode.Should().NotBeNullOrEmpty();

        await Task.CompletedTask; // placeholder — remove when activating
    }

    /// <summary>
    /// 5-03: Aynı ürün (aynı barkod) 5 platformda stok tutarlılığı kontrolü.
    /// Trendyol + Çiçeksepeti + Hepsiburada + Pazarama + N11 → stok farkı ±0 olmalı.
    /// </summary>
    [Fact(Skip = SkipReason)]
    public async Task CrossPlatformStock_SameBarcode_FivePlatforms_StockIsConsistent()
    {
        // const string testBarcode = "TEST-BARKOD-001"; // sandbox'ta var olan ürün barkodu
        // var tenantId = Guid.NewGuid();

        // var platforms = new[] { "TRENDYOL", "CICEKSEPETI", "HEPSIBURADA", "PAZARAMA", "N11" };
        // var stockPerPlatform = new Dictionary<string, int>();

        // foreach (var platform in platforms)
        // {
        //     var adapter = AdapterFactory.CreateFromEnv(platform);
        //     var product = await adapter.GetProductByBarcodeAsync(tenantId, testBarcode);
        //     if (product != null)
        //         stockPerPlatform[platform] = product.Stock;
        // }

        // stockPerPlatform.Should().NotBeEmpty("en az 1 platformda ürün bulunmalı");
        // var distinctStocks = stockPerPlatform.Values.Distinct().Count();
        // distinctStocks.Should().Be(1,
        //     $"tüm platformlarda stok eşit olmalı: {string.Join(", ", stockPerPlatform.Select(kv => $"{kv.Key}={kv.Value}"))}");

        await Task.CompletedTask; // placeholder — remove when activating
    }
}
