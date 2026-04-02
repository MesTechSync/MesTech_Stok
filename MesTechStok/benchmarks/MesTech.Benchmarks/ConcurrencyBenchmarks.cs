using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MesTech.Infrastructure.Persistence;

namespace MesTech.Benchmarks;

/// <summary>
/// Concurrent request benchmarks — Z15 (overselling koruma) contention testi.
/// Calistirma: dotnet run -c Release -- --filter *ConcurrencyBenchmarks*
/// DEV6 TUR: Alan genisleme D — performance profiling.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90, warmupCount: 2, iterationCount: 10)]
[RankColumn]
public class ConcurrencyBenchmarks : IDisposable
{
    private WebApplicationFactory<global::Program> _factory = null!;
    private HttpClient _client = null!;
    private Guid _tenantId;

    [Params(1, 5, 10, 25)]
    public int ConcurrentRequests { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _tenantId = Guid.NewGuid();

        _factory = new WebApplicationFactory<global::Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase($"ConcBenchDb-{Guid.NewGuid()}"));

                    services.AddAuthentication("TestScheme")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                            "TestScheme", _ => { });
                    services.PostConfigure<AuthenticationOptions>(o =>
                    {
                        o.DefaultAuthenticateScheme = "TestScheme";
                        o.DefaultChallengeScheme = "TestScheme";
                    });
                });
            });

        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-API-Key", "benchmark-test-key");
        _client.DefaultRequestHeaders.Add("X-Tenant-Id", _tenantId.ToString());
    }

    /// <summary>
    /// N concurrent GET /api/v1/products — okuma contention.
    /// Z15 zinciri — overselling koruma okuma tarafı.
    /// Hedef: Linear scale (&lt; N × single-request time × 1.5)
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task Concurrent_GET_Products()
    {
        var tasks = Enumerable.Range(0, ConcurrentRequests)
            .Select(_ => _client.GetAsync(
                $"/api/v1/products?tenantId={_tenantId}&page=1&pageSize=50"));
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// N concurrent GET /api/v1/stock/inventory — stok okuma contention.
    /// Hedef: Linear scale
    /// </summary>
    [Benchmark]
    public async Task Concurrent_GET_Stock_Inventory()
    {
        var tasks = Enumerable.Range(0, ConcurrentRequests)
            .Select(_ => _client.GetAsync(
                $"/api/v1/stock/inventory?tenantId={_tenantId}&page=1&pageSize=50"));
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// N concurrent GET /api/v1/orders/list — siparis okuma contention.
    /// Z11 zinciri — gecikme tespiti icin response time onemli.
    /// Hedef: Linear scale
    /// </summary>
    [Benchmark]
    public async Task Concurrent_GET_Orders()
    {
        var tasks = Enumerable.Range(0, ConcurrentRequests)
            .Select(_ => _client.GetAsync(
                $"/api/v1/orders/list?tenantId={_tenantId}"));
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// N concurrent GET /api/v1/dashboard/summary — dashboard contention.
    /// Production'da en çok çağrılan endpoint — cache etkinliği ölçümü.
    /// Hedef: Sub-linear (cache hit → O(1))
    /// </summary>
    [Benchmark]
    public async Task Concurrent_GET_Dashboard()
    {
        var tasks = Enumerable.Range(0, ConcurrentRequests)
            .Select(_ => _client.GetAsync(
                $"/api/v1/dashboard/summary?tenantId={_tenantId}"));
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// N concurrent mixed workload — gercekci production senaryosu.
    /// %40 products + %30 orders + %20 stock + %10 dashboard.
    /// </summary>
    [Benchmark]
    public async Task Concurrent_Mixed_Workload()
    {
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < ConcurrentRequests; i++)
        {
            var url = (i % 10) switch
            {
                < 4 => $"/api/v1/products?tenantId={_tenantId}&page=1&pageSize=50",
                < 7 => $"/api/v1/orders/list?tenantId={_tenantId}",
                < 9 => $"/api/v1/stock/inventory?tenantId={_tenantId}&page=1&pageSize=50",
                _ => $"/api/v1/dashboard/summary?tenantId={_tenantId}"
            };
            tasks.Add(_client.GetAsync(url));
        }
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Health endpoint under load — baseline overhead measurement.
    /// Hedef: Constant (&lt; 10ms × N)
    /// </summary>
    [Benchmark]
    public async Task Concurrent_GET_Health()
    {
        var tasks = Enumerable.Range(0, ConcurrentRequests)
            .Select(_ => _client.GetAsync("/health"));
        await Task.WhenAll(tasks);
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
        GC.SuppressFinalize(this);
    }
}
