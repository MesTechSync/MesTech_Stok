using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MesTech.Infrastructure.Persistence;

namespace MesTech.Benchmarks;

/// <summary>
/// Platform sync, dashboard, ve CRM endpoint benchmarks — Z9 (stok sync), Z8 (platform pasif).
/// Calistirma: dotnet run -c Release -- --filter *PlatformSyncBenchmarks*
/// DEV6 TUR: Alan genisleme D — performance profiling.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90, warmupCount: 3, iterationCount: 20)]
[RankColumn]
public class PlatformSyncBenchmarks : IDisposable
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
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase($"SyncBenchDb-{Guid.NewGuid()}"));

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

    // ═══ Platform Sync — Z9 (stok degisim → 11 platforma sync) ═══

    /// <summary>
    /// GET /api/v1/sync/status — tum platform sync durumu.
    /// Z9 zinciri kontrol noktasi.
    /// Hedef: &lt; 100ms
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<HttpResponseMessage> GET_Sync_Status()
    {
        return await _client.GetAsync($"/api/v1/sync/status?tenantId={_tenantId}");
    }

    /// <summary>
    /// GET /api/v1/platforms — aktif platform listesi.
    /// Hedef: &lt; 50ms
    /// </summary>
    [Benchmark]
    public async Task<HttpResponseMessage> GET_Platforms_List()
    {
        return await _client.GetAsync($"/api/v1/platforms?tenantId={_tenantId}");
    }

    /// <summary>
    /// GET /api/v1/stores — magaza listesi (tum platformlar).
    /// Hedef: &lt; 100ms
    /// </summary>
    [Benchmark]
    public async Task<HttpResponseMessage> GET_Stores_List()
    {
        return await _client.GetAsync($"/api/v1/stores?tenantId={_tenantId}");
    }

    // ═══ Dashboard — Ana sayfa widget'lari ═══

    /// <summary>
    /// GET /api/v1/dashboard/summary — ana dashboard ozet.
    /// Hedef: &lt; 200ms (cache hit: &lt; 50ms)
    /// </summary>
    [Benchmark]
    public async Task<HttpResponseMessage> GET_Dashboard_Summary()
    {
        return await _client.GetAsync($"/api/v1/dashboard/summary?tenantId={_tenantId}");
    }

    /// <summary>
    /// GET /api/v1/dashboard/widgets — dinamik widget verileri.
    /// Hedef: &lt; 200ms
    /// </summary>
    [Benchmark]
    public async Task<HttpResponseMessage> GET_Dashboard_Widgets()
    {
        return await _client.GetAsync($"/api/v1/dashboard/widgets?tenantId={_tenantId}");
    }

    // ═══ CRM — Musteri yonetimi ═══

    /// <summary>
    /// GET /api/v1/crm/customers — musteri listesi.
    /// Hedef: &lt; 200ms
    /// </summary>
    [Benchmark]
    public async Task<HttpResponseMessage> GET_Crm_Customers()
    {
        return await _client.GetAsync($"/api/v1/crm/customers?tenantId={_tenantId}&page=1&pageSize=50");
    }

    /// <summary>
    /// GET /api/v1/crm/dashboard — CRM dashboard.
    /// Hedef: &lt; 200ms
    /// </summary>
    [Benchmark]
    public async Task<HttpResponseMessage> GET_Crm_Dashboard()
    {
        return await _client.GetAsync($"/api/v1/crm/dashboard?tenantId={_tenantId}");
    }

    // ═══ Kargo — Z7 (kargo → gider GL 760.01) ═══

    /// <summary>
    /// GET /api/v1/shipments — kargo listesi.
    /// Z7 zinciri kontrol noktasi.
    /// Hedef: &lt; 200ms
    /// </summary>
    [Benchmark]
    public async Task<HttpResponseMessage> GET_Shipments_List()
    {
        return await _client.GetAsync($"/api/v1/shipments?tenantId={_tenantId}&page=1&pageSize=50");
    }

    // ═══ Dropshipping — ozel is modeli ═══

    /// <summary>
    /// GET /api/v1/dropshipping/dashboard — dropship dashboard.
    /// Hedef: &lt; 300ms
    /// </summary>
    [Benchmark]
    public async Task<HttpResponseMessage> GET_Dropship_Dashboard()
    {
        return await _client.GetAsync($"/api/v1/dropshipping/dashboard?tenantId={_tenantId}");
    }

    /// <summary>
    /// GET /api/v1/dropshipping/pool — urun havuzu.
    /// Hedef: &lt; 200ms
    /// </summary>
    [Benchmark]
    public async Task<HttpResponseMessage> GET_Dropship_Pool()
    {
        return await _client.GetAsync($"/api/v1/dropshipping/pool?tenantId={_tenantId}&page=1&pageSize=50");
    }

    // ═══ Kategori + Tedarikci ═══

    /// <summary>
    /// GET /api/v1/categories — kategori agaci.
    /// Hedef: &lt; 100ms (cache hit)
    /// </summary>
    [Benchmark]
    public async Task<HttpResponseMessage> GET_Categories_List()
    {
        return await _client.GetAsync($"/api/v1/categories?tenantId={_tenantId}");
    }

    /// <summary>
    /// GET /api/v1/suppliers — tedarikci listesi.
    /// Hedef: &lt; 100ms
    /// </summary>
    [Benchmark]
    public async Task<HttpResponseMessage> GET_Suppliers_List()
    {
        return await _client.GetAsync($"/api/v1/suppliers?tenantId={_tenantId}");
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
        GC.SuppressFinalize(this);
    }
}
