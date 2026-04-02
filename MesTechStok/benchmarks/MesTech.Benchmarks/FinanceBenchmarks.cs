using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MesTech.Infrastructure.Persistence;

namespace MesTech.Benchmarks;

/// <summary>
/// Finance &amp; Accounting endpoint benchmarks — muhasebe zincirleri (Z3, Z14).
/// Calistirma: dotnet run -c Release -- --filter *FinanceBenchmarks*
/// DEV6 TUR: Alan genisleme D — performance profiling.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90, warmupCount: 3, iterationCount: 20)]
[RankColumn]
public class FinanceBenchmarks : IDisposable
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
                        options.UseInMemoryDatabase($"FinanceBenchDb-{Guid.NewGuid()}"));

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

    // ═══ Muhasebe — Z3 (Fatura → GL yevmiye) + Z14 (Mizan) ═══

    /// <summary>
    /// GET /api/v1/accounting/trial-balance — mizan raporu (borc = alacak).
    /// Z14 zinciri — mali kontrol noktasi.
    /// Hedef: &lt; 300ms
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<HttpResponseMessage> GET_Accounting_TrialBalance()
    {
        return await _client.GetAsync($"/api/v1/accounting/trial-balance?tenantId={_tenantId}");
    }

    /// <summary>
    /// GET /api/v1/accounting/balance-sheet — bilanco.
    /// Hedef: &lt; 500ms
    /// </summary>
    [Benchmark]
    public async Task<HttpResponseMessage> GET_Accounting_BalanceSheet()
    {
        return await _client.GetAsync($"/api/v1/accounting/balance-sheet?tenantId={_tenantId}");
    }

    /// <summary>
    /// GET /api/v1/accounting/summary — muhasebe dashboard ozeti.
    /// Hedef: &lt; 200ms
    /// </summary>
    [Benchmark]
    public async Task<HttpResponseMessage> GET_Accounting_Summary()
    {
        return await _client.GetAsync($"/api/v1/accounting/summary?tenantId={_tenantId}");
    }

    /// <summary>
    /// GET /api/v1/accounting/chart-of-accounts — hesap plani listesi.
    /// Hedef: &lt; 100ms
    /// </summary>
    [Benchmark]
    public async Task<HttpResponseMessage> GET_Accounting_ChartOfAccounts()
    {
        return await _client.GetAsync($"/api/v1/accounting/chart-of-accounts?tenantId={_tenantId}");
    }

    /// <summary>
    /// GET /api/v1/accounting/fifo-cogs — FIFO maliyet hesaplama.
    /// Z3 zinciri — fatura → GL yevmiye (120/600/391).
    /// Hedef: &lt; 500ms
    /// </summary>
    [Benchmark]
    public async Task<HttpResponseMessage> GET_Accounting_FifoCogs()
    {
        return await _client.GetAsync($"/api/v1/accounting/fifo-cogs?tenantId={_tenantId}");
    }

    /// <summary>
    /// GET /api/v1/accounting/kdv-report — KDV raporu.
    /// Hedef: &lt; 300ms
    /// </summary>
    [Benchmark]
    public async Task<HttpResponseMessage> GET_Accounting_KdvReport()
    {
        return await _client.GetAsync($"/api/v1/accounting/kdv-report?tenantId={_tenantId}");
    }

    /// <summary>
    /// GET /api/v1/accounting/profit-report — kar raporu.
    /// Hedef: &lt; 300ms
    /// </summary>
    [Benchmark]
    public async Task<HttpResponseMessage> GET_Accounting_ProfitReport()
    {
        return await _client.GetAsync($"/api/v1/accounting/profit-report?tenantId={_tenantId}");
    }

    /// <summary>
    /// GET /api/v1/accounting/reconciliation-dashboard — mutabakat paneli.
    /// Hedef: &lt; 200ms
    /// </summary>
    [Benchmark]
    public async Task<HttpResponseMessage> GET_Accounting_ReconciliationDashboard()
    {
        return await _client.GetAsync($"/api/v1/accounting/reconciliation-dashboard?tenantId={_tenantId}");
    }

    // ═══ Finans — Gelir/Gider/Nakit ═══

    /// <summary>
    /// GET /api/v1/finance/profit-loss — aylik kar/zarar.
    /// Hedef: &lt; 300ms
    /// </summary>
    [Benchmark]
    public async Task<HttpResponseMessage> GET_Finance_ProfitLoss()
    {
        return await _client.GetAsync(
            $"/api/v1/finance/profit-loss?tenantId={_tenantId}&year=2026&month=3");
    }

    /// <summary>
    /// GET /api/v1/finance/cash-flow — nakit akisi.
    /// Hedef: &lt; 300ms
    /// </summary>
    [Benchmark]
    public async Task<HttpResponseMessage> GET_Finance_CashFlow()
    {
        return await _client.GetAsync(
            $"/api/v1/finance/cash-flow?tenantId={_tenantId}&year=2026&month=3");
    }

    /// <summary>
    /// GET /api/v1/finance/bank-accounts — banka hesaplari.
    /// Hedef: &lt; 100ms
    /// </summary>
    [Benchmark]
    public async Task<HttpResponseMessage> GET_Finance_BankAccounts()
    {
        return await _client.GetAsync($"/api/v1/finance/bank-accounts?tenantId={_tenantId}");
    }

    /// <summary>
    /// GET /api/v1/invoices — e-fatura listesi.
    /// Z3 zinciri destek — fatura kontrol noktasi.
    /// Hedef: &lt; 200ms
    /// </summary>
    [Benchmark]
    public async Task<HttpResponseMessage> GET_Invoices_List()
    {
        return await _client.GetAsync($"/api/v1/invoices?tenantId={_tenantId}&page=1&pageSize=50");
    }

    /// <summary>
    /// GET /api/v1/settlements — hesap kesim listesi.
    /// Z6 zinciri — komisyon → gider GL (760.XX).
    /// Hedef: &lt; 200ms
    /// </summary>
    [Benchmark]
    public async Task<HttpResponseMessage> GET_Settlements_List()
    {
        return await _client.GetAsync($"/api/v1/settlements?tenantId={_tenantId}&page=1&pageSize=50");
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
        GC.SuppressFinalize(this);
    }
}
