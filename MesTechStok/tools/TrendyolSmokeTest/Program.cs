using System.Text.Json;
using MesTech.Infrastructure.Integration.Adapters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// ═══════════════════════════════════════════════════════════════
// Trendyol Smoke Test + Full Sync Runner — G804 + DEV3 TUR4
// ═══════════════════════════════════════════════════════════════
// Kullanım:
//   dotnet run --project tools/TrendyolSmokeTest/                 → Smoke test (sandbox)
//   dotnet run --project tools/TrendyolSmokeTest/ -- --full-sync  → Full sync (production → DB)
// ═══════════════════════════════════════════════════════════════

if (args.Contains("--full-sync"))
    return await TrendyolFullSyncRunner.RunAsync();

Console.WriteLine("═══ Trendyol Sandbox Smoke Test ═══");

var apiKey = Environment.GetEnvironmentVariable("TRENDYOL_API_KEY");
var apiSecret = Environment.GetEnvironmentVariable("TRENDYOL_API_SECRET");
var supplierId = Environment.GetEnvironmentVariable("TRENDYOL_SUPPLIER_ID");

if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret) || string.IsNullOrWhiteSpace(supplierId))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("HATA: Credential environment variable'ları ayarlanmamış.");
    Console.WriteLine("  set TRENDYOL_API_KEY=...");
    Console.WriteLine("  set TRENDYOL_API_SECRET=...");
    Console.WriteLine("  set TRENDYOL_SUPPLIER_ID=...");
    Console.ResetColor();
    return 1;
}

var options = Options.Create(new TrendyolOptions { UseSandbox = true });
using var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
var logger = loggerFactory.CreateLogger<TrendyolAdapter>();

using var httpClient = new HttpClient();
var adapter = new TrendyolAdapter(httpClient, logger, options);

var credentials = new Dictionary<string, string>
{
    ["ApiKey"] = apiKey,
    ["ApiSecret"] = apiSecret,
    ["SupplierId"] = supplierId
};

// ═══ TEST 1: Bağlantı Testi ═══
Console.WriteLine("\n[TEST 1] Bağlantı testi...");
var connectionResult = await adapter.TestConnectionAsync(credentials);
Console.WriteLine($"  Sonuç: {(connectionResult.IsSuccess ? "✅ BAŞARILI" : "❌ BAŞARISIZ")}");
Console.WriteLine($"  Mağaza: {connectionResult.StoreName}");
Console.WriteLine($"  Süre: {connectionResult.ResponseTime.TotalMilliseconds:F0}ms");
if (!string.IsNullOrEmpty(connectionResult.ErrorMessage))
    Console.WriteLine($"  Hata: {connectionResult.ErrorMessage}");

if (!connectionResult.IsSuccess)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("\nBağlantı başarısız — diğer testler atlanıyor.");
    Console.ResetColor();
    return 1;
}

// ═══ TEST 2: Ürün Çekme ═══
Console.WriteLine("\n[TEST 2] Ürün çekme (ilk 5)...");
try
{
    var products = await adapter.PullProductsAsync(5);
    Console.WriteLine($"  Sonuç: ✅ {products.Count} ürün çekildi");
    foreach (var p in products.Take(3))
        Console.WriteLine($"    - {p.SKU}: {p.Name} | Stok={p.Stock} | Fiyat={p.SalePrice:F2}");
}
catch (Exception ex)
{
    Console.WriteLine($"  Sonuç: ❌ {ex.Message}");
}

// ═══ TEST 3: Sipariş Çekme ═══
Console.WriteLine("\n[TEST 3] Sipariş çekme (son 24 saat)...");
try
{
    var orders = await adapter.PullOrdersAsync(DateTime.UtcNow.AddHours(-24));
    Console.WriteLine($"  Sonuç: ✅ {orders.Count} sipariş çekildi");
    foreach (var o in orders.Take(3))
        Console.WriteLine($"    - {o.PlatformOrderId}: {o.Status} | Tutar={o.TotalAmount:F2}");
}
catch (Exception ex)
{
    Console.WriteLine($"  Sonuç: ❌ {ex.Message}");
}

// ═══ TEST 4: Kategori Çekme ═══
Console.WriteLine("\n[TEST 4] Kategori çekme...");
try
{
    var categories = await adapter.GetCategoriesAsync();
    Console.WriteLine($"  Sonuç: ✅ {categories.Count} kategori çekildi");
    foreach (var c in categories.Take(3))
        Console.WriteLine($"    - {c.PlatformCategoryId}: {c.Name}");
}
catch (Exception ex)
{
    Console.WriteLine($"  Sonuç: ❌ {ex.Message}");
}

// ═══ TEST 5: Sağlık Kontrolü ═══
Console.WriteLine("\n[TEST 5] Health check...");
try
{
    var health = await adapter.CheckHealthAsync();
    Console.WriteLine($"  Sonuç: ✅ Platform={health.PlatformCode} | Healthy={health.IsHealthy}");
}
catch (Exception ex)
{
    Console.WriteLine($"  Sonuç: ❌ {ex.Message}");
}

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("\n═══ Smoke Test Tamamlandı ═══");
Console.ResetColor();
return 0;
