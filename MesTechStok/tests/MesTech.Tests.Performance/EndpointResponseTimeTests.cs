using System.Diagnostics;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MesTech.Infrastructure.Persistence;
using MesTech.Tests.Performance.Benchmarks;
using Xunit;
using Xunit.Abstractions;

namespace MesTech.Tests.Performance;

/// <summary>
/// Endpoint response time tests — WebApplicationFactory + Stopwatch.
/// BenchmarkDotNet WebApi build uyumsuzlugu nedeniyle xUnit ile olcum.
/// DEV6 TUR: Performance profiling — production baseline.
/// </summary>
[Trait("Category", "Performance")]
[Trait("Layer", "Endpoint")]
public sealed class EndpointResponseTimeTests : IClassFixture<EndpointResponseTimeTests.TestWebAppFactory>, IDisposable
{
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;
    private readonly Guid _tenantId = Guid.NewGuid();

    public EndpointResponseTimeTests(TestWebAppFactory factory, ITestOutputHelper output)
    {
        _output = output;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-API-Key", "perf-test-key");
        _client.DefaultRequestHeaders.Add("X-Tenant-Id", _tenantId.ToString());
    }

    // ═══ P0 — Kritik endpoint'ler ═══

    [Fact]
    public async Task GET_Health_ShouldRespondUnder50ms()
    {
        await WarmUp("/health");
        var (elapsed, status) = await MeasureAsync("/health");

        _output.WriteLine($"GET /health → {status} in {elapsed.TotalMilliseconds:F1}ms");
        elapsed.TotalMilliseconds.Should().BeLessThan(50, "health endpoint must be fast");
    }

    [Fact]
    public async Task GET_Products_ShouldRespondUnder200ms()
    {
        await WarmUp($"/api/v1/products?tenantId={_tenantId}&page=1&pageSize=50");
        var (elapsed, status) = await MeasureAsync($"/api/v1/products?tenantId={_tenantId}&page=1&pageSize=50");

        _output.WriteLine($"GET /products → {status} in {elapsed.TotalMilliseconds:F1}ms");
        elapsed.TotalMilliseconds.Should().BeLessThan(200, "product list is a hot path");
    }

    [Fact]
    public async Task GET_Orders_ShouldRespondUnder200ms()
    {
        await WarmUp($"/api/v1/orders/list?tenantId={_tenantId}");
        var (elapsed, status) = await MeasureAsync($"/api/v1/orders/list?tenantId={_tenantId}");

        _output.WriteLine($"GET /orders → {status} in {elapsed.TotalMilliseconds:F1}ms");
        elapsed.TotalMilliseconds.Should().BeLessThan(200);
    }

    [Fact]
    public async Task GET_StockInventory_ShouldRespondUnder200ms()
    {
        await WarmUp($"/api/v1/stock/inventory?tenantId={_tenantId}&page=1&pageSize=50");
        var (elapsed, status) = await MeasureAsync($"/api/v1/stock/inventory?tenantId={_tenantId}&page=1&pageSize=50");

        _output.WriteLine($"GET /stock/inventory → {status} in {elapsed.TotalMilliseconds:F1}ms");
        elapsed.TotalMilliseconds.Should().BeLessThan(200);
    }

    [Fact]
    public async Task GET_StockStatistics_ShouldRespondUnder100ms()
    {
        await WarmUp($"/api/v1/stock/statistics?tenantId={_tenantId}");
        var (elapsed, status) = await MeasureAsync($"/api/v1/stock/statistics?tenantId={_tenantId}");

        _output.WriteLine($"GET /stock/statistics → {status} in {elapsed.TotalMilliseconds:F1}ms");
        elapsed.TotalMilliseconds.Should().BeLessThan(100);
    }

    // ═══ P1 — Rapor endpoint'leri ═══

    [Fact]
    public async Task GET_Reports_Profitability_ShouldRespondUnder500ms()
    {
        var url = $"/api/v1/reports/profitability?tenantId={_tenantId}&startDate=2026-01-01&endDate=2026-03-31";
        await WarmUp(url);
        var (elapsed, status) = await MeasureAsync(url);

        _output.WriteLine($"GET /reports/profitability → {status} in {elapsed.TotalMilliseconds:F1}ms");
        elapsed.TotalMilliseconds.Should().BeLessThan(500);
    }

    [Fact]
    public async Task GET_Accounting_TrialBalance_ShouldRespondUnder500ms()
    {
        var url = $"/api/v1/accounting/trial-balance?tenantId={_tenantId}";
        await WarmUp(url);
        var (elapsed, status) = await MeasureAsync(url);

        _output.WriteLine($"GET /accounting/trial-balance → {status} in {elapsed.TotalMilliseconds:F1}ms");
        elapsed.TotalMilliseconds.Should().BeLessThan(500);
    }

    [Fact]
    public async Task GET_Dashboard_Summary_ShouldRespondUnder200ms()
    {
        var url = $"/api/v1/dashboard/summary?tenantId={_tenantId}";
        await WarmUp(url);
        var (elapsed, status) = await MeasureAsync(url);

        _output.WriteLine($"GET /dashboard/summary → {status} in {elapsed.TotalMilliseconds:F1}ms");
        elapsed.TotalMilliseconds.Should().BeLessThan(200);
    }

    // ═══ P1 — Finans endpoint'leri ═══

    [Fact]
    public async Task GET_Finance_ProfitLoss_ShouldRespondUnder500ms()
    {
        var url = $"/api/v1/finance/profit-loss?tenantId={_tenantId}&year=2026&month=3";
        await WarmUp(url);
        var (elapsed, status) = await MeasureAsync(url);

        _output.WriteLine($"GET /finance/profit-loss → {status} in {elapsed.TotalMilliseconds:F1}ms");
        elapsed.TotalMilliseconds.Should().BeLessThan(500);
    }

    [Fact]
    public async Task GET_Accounting_FifoCogs_ShouldRespondUnder500ms()
    {
        var url = $"/api/v1/accounting/fifo-cogs?tenantId={_tenantId}";
        await WarmUp(url);
        var (elapsed, status) = await MeasureAsync(url);

        _output.WriteLine($"GET /accounting/fifo-cogs → {status} in {elapsed.TotalMilliseconds:F1}ms");
        elapsed.TotalMilliseconds.Should().BeLessThan(500);
    }

    // ═══ Concurrent — Yük altında davranış ═══

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(25)]
    public async Task Concurrent_GET_Products_ShouldScaleLinearly(int concurrency)
    {
        await WarmUp($"/api/v1/products?tenantId={_tenantId}&page=1&pageSize=50");

        var sw = Stopwatch.StartNew();
        var tasks = Enumerable.Range(0, concurrency)
            .Select(_ => _client.GetAsync($"/api/v1/products?tenantId={_tenantId}&page=1&pageSize=50"));
        var results = await Task.WhenAll(tasks);
        sw.Stop();

        var avgMs = sw.Elapsed.TotalMilliseconds / concurrency;
        _output.WriteLine($"Concurrent({concurrency}) GET /products → avg {avgMs:F1}ms, total {sw.Elapsed.TotalMilliseconds:F1}ms");

        // Linear scale tolerance: total < single × concurrency × 2
        sw.Elapsed.TotalMilliseconds.Should().BeLessThan(200 * concurrency * 2);
    }

    // ═══ Helpers ═══

    private async Task WarmUp(string url)
    {
        try { await _client.GetAsync(url); } catch { /* warmup, ignore */ }
    }

    private async Task<(TimeSpan Elapsed, int StatusCode)> MeasureAsync(string url, int iterations = 5)
    {
        var times = new List<TimeSpan>(iterations);
        int lastStatus = 0;

        for (int i = 0; i < iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            var response = await _client.GetAsync(url);
            sw.Stop();
            times.Add(sw.Elapsed);
            lastStatus = (int)response.StatusCode;
        }

        // Median of 5 runs
        times.Sort();
        return (times[iterations / 2], lastStatus);
    }

    public void Dispose() => _client?.Dispose();

    // ═══ Test Factory ═══

    public sealed class TestWebAppFactory : WebApplicationFactory<global::Program>
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase($"PerfTestDb-{Guid.NewGuid()}"));

                services.AddAuthentication("TestScheme")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        "TestScheme", _ => { });
                services.PostConfigure<AuthenticationOptions>(o =>
                {
                    o.DefaultAuthenticateScheme = "TestScheme";
                    o.DefaultChallengeScheme = "TestScheme";
                });
            });
        }
    }
}
