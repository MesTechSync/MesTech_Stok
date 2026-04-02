using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace MesTech.Benchmarks;

/// <summary>
/// Adapter katmani mikro-benchmark'lari.
/// Calistirma: dotnet run -c Release -- --filter *AdapterBenchmarks*
/// DEV3 TUR5: Alan genisleme B — performans profiling.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
[RankColumn]
public class AdapterBenchmarks
{
    private byte[] _settlementJsonPayload = null!;
    private byte[] _webhookPayload = null!;
    private byte[] _hmacKey = null!;
    private string _precomputedSignature = null!;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [GlobalSetup]
    public void Setup()
    {
        // Settlement parse benchmark payload — 100 satirlik tipik hesap kesimi
        var settlementLines = new List<object>();
        for (int i = 0; i < 100; i++)
        {
            settlementLines.Add(new
            {
                orderId = $"ORD-{i:D6}",
                platform = "Trendyol",
                amount = 149.90m + i,
                commission = 14.99m + (i * 0.1m),
                netAmount = 134.91m + (i * 0.9m),
                currency = "TRY",
                transactionDate = DateTime.UtcNow.AddDays(-i).ToString("O")
            });
        }
        _settlementJsonPayload = JsonSerializer.SerializeToUtf8Bytes(
            new { content = settlementLines }, _jsonOptions);

        // Webhook HMAC benchmark payload
        _webhookPayload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            eventType = "OrderCreated",
            orderId = "ORD-000001",
            platform = "Trendyol",
            timestamp = DateTime.UtcNow.ToString("O")
        }, _jsonOptions);

        _hmacKey = Encoding.UTF8.GetBytes("test-webhook-secret-key-32-bytes!");
        using var hmac = new HMACSHA256(_hmacKey);
        var hash = hmac.ComputeHash(_webhookPayload);
        _precomputedSignature = Convert.ToHexStringLower(hash);
    }

    /// <summary>
    /// JSON deserialization — 100 satirlik settlement payload.
    /// Hedef: &lt; 500us
    /// </summary>
    [Benchmark(Baseline = true)]
    public object? Settlement_Deserialize_100Lines()
    {
        return JsonSerializer.Deserialize<JsonDocument>(
            _settlementJsonPayload, _jsonOptions);
    }

    /// <summary>
    /// JSON deserialization + alan mapping — tipik settlement parse akisi.
    /// Hedef: &lt; 1ms
    /// </summary>
    [Benchmark]
    public List<SettlementLine> Settlement_ParseAndMap_100Lines()
    {
        using var doc = JsonSerializer.Deserialize<JsonDocument>(
            _settlementJsonPayload, _jsonOptions)!;

        var lines = new List<SettlementLine>(100);
        foreach (var element in doc.RootElement.GetProperty("content").EnumerateArray())
        {
            lines.Add(new SettlementLine
            {
                OrderId = element.GetProperty("orderId").GetString()!,
                Amount = element.GetProperty("amount").GetDecimal(),
                Commission = element.GetProperty("commission").GetDecimal(),
                NetAmount = element.GetProperty("netAmount").GetDecimal()
            });
        }
        return lines;
    }

    /// <summary>
    /// HMAC-SHA256 hesaplama — webhook imza dogrulama hot path.
    /// Hedef: &lt; 50us
    /// </summary>
    [Benchmark]
    public byte[] Webhook_HMAC_Compute()
    {
        using var hmac = new HMACSHA256(_hmacKey);
        return hmac.ComputeHash(_webhookPayload);
    }

    /// <summary>
    /// HMAC-SHA256 + FixedTimeEquals — tam webhook dogrulama.
    /// Hedef: &lt; 100us
    /// </summary>
    [Benchmark]
    public bool Webhook_HMAC_Verify()
    {
        using var hmac = new HMACSHA256(_hmacKey);
        var computed = hmac.ComputeHash(_webhookPayload);
        var expected = Convert.FromHexString(_precomputedSignature);
        return CryptographicOperations.FixedTimeEquals(computed, expected);
    }

    /// <summary>
    /// SHA256 hash hesaplama — settlement idempotency kontrol.
    /// Hedef: &lt; 200us (100 satir payload icin)
    /// </summary>
    [Benchmark]
    public string Settlement_HashCompute()
    {
        var hash = SHA256.HashData(_settlementJsonPayload);
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>
    /// Toplu stok guncelleme JSON serialization — 1000 SKU batch.
    /// Hedef: &lt; 2ms
    /// </summary>
    [Benchmark]
    public byte[] StockUpdate_Serialize_1000SKU()
    {
        var items = new List<object>(1000);
        for (int i = 0; i < 1000; i++)
        {
            items.Add(new
            {
                barcode = $"SKU-{i:D6}",
                quantity = 10 + (i % 100),
                listPrice = 99.90m + i,
                salePrice = 89.90m + i
            });
        }
        return JsonSerializer.SerializeToUtf8Bytes(new { items }, _jsonOptions);
    }

    /// <summary>
    /// Platform code resolution — adapter factory lookup simulasyonu.
    /// Production'daki singleton dictionary lookup'i olcer.
    /// Hedef: &lt; 100ns
    /// </summary>
    private static readonly Dictionary<string, string> _platformRegistry = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Trendyol"] = "TrendyolAdapter",
        ["Hepsiburada"] = "HepsiburadaAdapter",
        ["N11"] = "N11Adapter",
        ["Ciceksepeti"] = "CiceksepetiAdapter",
        ["Amazon"] = "AmazonTrAdapter",
        ["eBay"] = "EbayAdapter",
        ["Ozon"] = "OzonAdapter",
        ["Shopify"] = "ShopifyAdapter",
        ["WooCommerce"] = "WooCommerceAdapter",
        ["Zalando"] = "ZalandoAdapter",
        ["OpenCart"] = "OpenCartAdapter",
        ["PttAvm"] = "PttAvmAdapter",
        ["Pazarama"] = "PazaramaAdapter",
        ["Etsy"] = "EtsyAdapter",
        ["AmazonEu"] = "AmazonEuAdapter",
        ["Bitrix24"] = "Bitrix24Adapter"
    };

    [Benchmark]
    public string? AdapterFactory_Resolve()
    {
        return _platformRegistry.GetValueOrDefault("Trendyol");
    }
}

public class SettlementLine
{
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Commission { get; set; }
    public decimal NetAmount { get; set; }
}
