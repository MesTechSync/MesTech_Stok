using System.Diagnostics;

// ═══════════════════════════════════════════════════════════════
// AdapterConnectivityCheck — 16 adapter ping testi
// ═══════════════════════════════════════════════════════════════
// Kullanım: dotnet run --project tools/AdapterConnectivityCheck/
// NOT: Credential olmadan çalışır — sadece DNS+TCP+HTTP erişim testi
// ═══════════════════════════════════════════════════════════════

Console.WriteLine("═══ Adapter Connectivity Check (16 platform) ═══\n");

// Credential olmadan HEAD request gönderir — 401/403 = reachable
var platforms = new (string name, string baseUrl)[]
{
    ("Trendyol", "https://apigw.trendyol.com"),
    ("Hepsiburada", "https://mpop.hepsiburada.com"),
    ("N11", "https://api.n11.com"),
    ("Ciceksepeti", "https://apis.ciceksepeti.com"),
    ("Pazarama", "https://isortagimgiris.pazarama.com"),
    ("Amazon TR", "https://sellingpartnerapi-eu.amazon.com"),
    ("Amazon EU", "https://sellingpartnerapi-eu.amazon.com"),
    ("eBay", "https://api.ebay.com"),
    ("Ozon", "https://api-seller.ozon.ru"),
    ("Etsy", "https://openapi.etsy.com"),
    ("Shopify", "https://myshopify.com"),
    ("WooCommerce", "https://woocommerce.com"),
    ("Zalando", "https://api.zalando.com"),
    ("PttAVM", "https://apigw.pttavm.com"),
    ("OpenCart", "https://mestech.app"), // self-hosted — configurable
    ("Bitrix24", "https://mestech.bitrix24.com.tr"),
};

using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
var results = new List<(string platform, bool ok, double ms, string? error)>();

Console.WriteLine($"{"Platform",-18} {"Durum",-12} {"Süre",-10} {"Detay"}");
Console.WriteLine(new string('─', 70));

foreach (var (name, url) in platforms)
{
    var sw = Stopwatch.StartNew();
    try
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        var request = new HttpRequestMessage(HttpMethod.Head, url);
        using var response = await httpClient.SendAsync(request, cts.Token);
        sw.Stop();

        // Any HTTP response = reachable (even 401/403/404)
        var status = response.IsSuccessStatusCode ? "✅ OK" : $"✅ {(int)response.StatusCode}";
        results.Add((name, true, sw.Elapsed.TotalMilliseconds, $"HTTP {(int)response.StatusCode}"));
        Console.WriteLine($"  {name,-16} {status,-12} {sw.Elapsed.TotalMilliseconds,6:F0}ms   {response.StatusCode}");
    }
    catch (TaskCanceledException)
    {
        sw.Stop();
        results.Add((name, false, sw.Elapsed.TotalMilliseconds, "TIMEOUT"));
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  {name,-16} {"❌ TIMEOUT",-12} {sw.Elapsed.TotalMilliseconds,6:F0}ms");
        Console.ResetColor();
    }
    catch (HttpRequestException ex)
    {
        sw.Stop();
        results.Add((name, false, sw.Elapsed.TotalMilliseconds, ex.Message));
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  {name,-16} {"❌ FAIL",-12} {sw.Elapsed.TotalMilliseconds,6:F0}ms   {ex.Message[..Math.Min(50, ex.Message.Length)]}");
        Console.ResetColor();
    }
    catch (Exception ex)
    {
        sw.Stop();
        results.Add((name, false, sw.Elapsed.TotalMilliseconds, ex.Message));
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  {name,-16} {"❌ ERROR",-12} {sw.Elapsed.TotalMilliseconds,6:F0}ms   {ex.GetType().Name}");
        Console.ResetColor();
    }
}

// Summary
var reachable = results.Count(r => r.ok);
var unreachable = results.Count(r => !r.ok);
var avgMs = results.Where(r => r.ok).Select(r => r.ms).DefaultIfEmpty(0).Average();

Console.WriteLine(new string('─', 70));
Console.ForegroundColor = reachable == results.Count ? ConsoleColor.Green : ConsoleColor.Yellow;
Console.WriteLine($"\n  SONUÇ: {reachable}/{results.Count} erişilebilir | {unreachable} UNREACHABLE | Ort. {avgMs:F0}ms");
Console.ResetColor();

// Write report file
var reportPath = "connectivity_report.txt";
var reportLines = new List<string>
{
    $"Adapter Connectivity Report — {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}",
    $"Reachable: {reachable}/{results.Count}",
    $"Average latency: {avgMs:F0}ms",
    ""
};
foreach (var r in results)
    reportLines.Add($"{r.platform,-18} {(r.ok ? "OK" : "FAIL"),-6} {r.ms,6:F0}ms  {r.error}");

await File.WriteAllLinesAsync(reportPath, reportLines);
Console.WriteLine($"\n  Rapor: {Path.GetFullPath(reportPath)}");

return unreachable > 0 ? 1 : 0;
