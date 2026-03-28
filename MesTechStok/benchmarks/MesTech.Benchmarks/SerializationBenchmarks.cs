using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Benchmarks;

/// <summary>
/// JSON serialization benchmark'lari — API response hot path.
/// Calistirma: dotnet run -c Release -- --filter *SerializationBenchmarks*
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
[RankColumn]
public class SerializationBenchmarks
{
    private static readonly JsonSerializerOptions CamelCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private List<Product> _products = null!;
    private string _serializedJson = null!;

    [Params(10, 100, 1000)]
    public int ProductCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var tenantId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        _products = Enumerable.Range(0, ProductCount).Select(i => new Product
        {
            TenantId = tenantId,
            Barcode = $"8690000{i:D6}",
            Name = $"Test Product {i}",
            SKU = $"SKU-{i:D6}",
            CategoryId = categoryId,
            PurchasePrice = 50.00m + i,
            SalePrice = 100.00m + i,
            Stock = 100
        }).ToList();

        _serializedJson = JsonSerializer.Serialize(_products, CamelCase);
    }

    /// <summary>
    /// Product listesi serialize — API response oluşturma.
    /// </summary>
    [Benchmark(Baseline = true)]
    public string Serialize_Products()
    {
        return JsonSerializer.Serialize(_products, CamelCase);
    }

    /// <summary>
    /// Product listesi deserialize — API request parse.
    /// </summary>
    [Benchmark]
    public List<Product>? Deserialize_Products()
    {
        return JsonSerializer.Deserialize<List<Product>>(_serializedJson, CamelCase);
    }
}
