using System.Net.Http.Headers;
using System.Net.Http.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MesTech.Infrastructure.Persistence;

namespace MesTech.Benchmarks;

/// <summary>
/// WebApi endpoint response time benchmarks — BenchmarkDotNet + WebApplicationFactory.
/// JWT auth bypassed via TestAuthHandler; InMemory DB for isolation.
/// Çalıştırma: dotnet run -c Release -- --filter *EndpointBenchmarks*
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90, warmupCount: 3, iterationCount: 20)]
[RankColumn]
public class EndpointBenchmarks : IDisposable
{
    private WebApplicationFactory<global::Program> _factory = null!;
    private HttpClient _client = null!;
    private Guid _tenantId;

    [GlobalSetup]
    public void Setup()
    {
        _tenantId = Guid.NewGuid();

        _factory = new WebApplicationFactory<global::Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove real DbContext and replace with InMemory
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase($"BenchmarkDb-{Guid.NewGuid()}"));

                    // Bypass JWT auth with test scheme
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

    // ═══ P0 — En kritik endpoint'ler (production impact) ═══

    /// <summary>
    /// GET /api/v1/products — paginated product list.
    /// Hedef: &lt; 50ms (cache hit), &lt; 200ms (cache miss)
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<HttpResponseMessage> GET_Products_List()
    {
        return await _client.GetAsync($"/api/v1/products?tenantId={_tenantId}&page=1&pageSize=50");
    }

    /// <summary>
    /// GET /api/v1/orders/list — tenant-scoped order list.
    /// Hedef: &lt; 100ms
    /// </summary>
    [Benchmark]
    public async Task<HttpResponseMessage> GET_Orders_List()
    {
        return await _client.GetAsync($"/api/v1/orders/list?tenantId={_tenantId}");
    }

    /// <summary>
    /// GET /api/v1/stock/inventory — paged inventory.
    /// Hedef: &lt; 100ms
    /// </summary>
    [Benchmark]
    public async Task<HttpResponseMessage> GET_Stock_Inventory()
    {
        return await _client.GetAsync($"/api/v1/stock/inventory?tenantId={_tenantId}&page=1&pageSize=50");
    }

    /// <summary>
    /// GET /api/v1/stock/statistics — inventory stats (dashboard widget).
    /// Hedef: &lt; 50ms
    /// </summary>
    [Benchmark]
    public async Task<HttpResponseMessage> GET_Stock_Statistics()
    {
        return await _client.GetAsync($"/api/v1/stock/statistics?tenantId={_tenantId}");
    }

    /// <summary>
    /// GET /api/v1/stock/summary — stock summary.
    /// Hedef: &lt; 50ms
    /// </summary>
    [Benchmark]
    public async Task<HttpResponseMessage> GET_Stock_Summary()
    {
        return await _client.GetAsync($"/api/v1/stock/summary?tenantId={_tenantId}");
    }

    // ═══ P1 — Ağır hesaplama (report) endpoint'leri ═══

    /// <summary>
    /// GET /api/v1/reports/profitability — COGS + commission + cargo - VAT.
    /// Hedef: &lt; 500ms
    /// </summary>
    [Benchmark]
    public async Task<HttpResponseMessage> GET_Reports_Profitability()
    {
        return await _client.GetAsync(
            $"/api/v1/reports/profitability?tenantId={_tenantId}&startDate=2026-01-01&endDate=2026-03-31");
    }

    /// <summary>
    /// GET /api/v1/reports/monthly-summary — period-end calculation.
    /// Hedef: &lt; 300ms
    /// </summary>
    [Benchmark]
    public async Task<HttpResponseMessage> GET_Reports_MonthlySummary()
    {
        return await _client.GetAsync($"/api/v1/reports/monthly-summary/2026/3?tenantId={_tenantId}");
    }

    /// <summary>
    /// GET /api/v1/stock/value-report — FIFO/COGS valuation.
    /// Hedef: &lt; 500ms
    /// </summary>
    [Benchmark]
    public async Task<HttpResponseMessage> GET_Stock_ValueReport()
    {
        return await _client.GetAsync($"/api/v1/stock/value-report?tenantId={_tenantId}");
    }

    /// <summary>
    /// GET /api/v1/reports/platform-comparison — multi-platform sales comparison.
    /// Hedef: &lt; 300ms
    /// </summary>
    [Benchmark]
    public async Task<HttpResponseMessage> GET_Reports_PlatformComparison()
    {
        return await _client.GetAsync(
            $"/api/v1/reports/platform-comparison?tenantId={_tenantId}&startDate=2026-01-01&endDate=2026-03-31");
    }

    // ═══ P1 — Yazma endpoint'leri (contention test) ═══

    /// <summary>
    /// GET /api/v1/products/prices — product prices for bulk update UI.
    /// Hedef: &lt; 100ms
    /// </summary>
    [Benchmark]
    public async Task<HttpResponseMessage> GET_Products_Prices()
    {
        return await _client.GetAsync($"/api/v1/products/prices?tenantId={_tenantId}&page=1&pageSize=50");
    }

    // ═══ Health / baseline ═══

    /// <summary>
    /// GET /health — baseline (AllowAnonymous, no DB).
    /// Hedef: &lt; 10ms
    /// </summary>
    [Benchmark]
    public async Task<HttpResponseMessage> GET_Health()
    {
        return await _client.GetAsync("/health");
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
        GC.SuppressFinalize(this);
    }
}
