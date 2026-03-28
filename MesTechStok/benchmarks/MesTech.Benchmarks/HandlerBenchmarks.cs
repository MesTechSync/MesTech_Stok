using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MesTech.Domain.Accounting.Services;

namespace MesTech.Benchmarks;

/// <summary>
/// Domain seviye mikro-benchmark'lar.
/// Calistirma: dotnet run -c Release -- --filter *HandlerBenchmarks*
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
[RankColumn]
public class HandlerBenchmarks
{
    private CommissionCalculationService _commissionService = null!;
    private readonly string[] _platforms =
    [
        "Trendyol", "Hepsiburada", "N11", "Ciceksepeti",
        "Amazon", "eBay", "Ozon", "Shopify", "OpenCart"
    ];

    [GlobalSetup]
    public void Setup()
    {
        _commissionService = new CommissionCalculationService();
    }

    /// <summary>
    /// Tek platform komisyon hesaplama — sıcak yol.
    /// Hedef: &lt; 100ns
    /// </summary>
    [Benchmark(Baseline = true)]
    public decimal Commission_SinglePlatform()
    {
        return _commissionService.CalculateCommission("Trendyol", null, 150.00m);
    }

    /// <summary>
    /// 1000 ürün × 9 platform komisyon hesaplama — toplu işlem senaryosu.
    /// Hedef: &lt; 1ms
    /// </summary>
    [Benchmark]
    public decimal Commission_1000Products_9Platforms()
    {
        decimal total = 0;
        for (int i = 0; i < 1000; i++)
        {
            foreach (var platform in _platforms)
            {
                total += _commissionService.CalculateCommission(platform, null, 100.00m + i);
            }
        }
        return total;
    }

    /// <summary>
    /// Bilinmeyen platform fallback — dictionary miss senaryosu.
    /// </summary>
    [Benchmark]
    public decimal Commission_UnknownPlatform_Fallback()
    {
        return _commissionService.CalculateCommission("UnknownMarketplace", null, 200.00m);
    }
}
